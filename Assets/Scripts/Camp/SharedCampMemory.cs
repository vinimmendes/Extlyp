using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.IO.MemoryMappedFiles;
using System.Threading;
using Newtonsoft.Json.Linq;
#endif

namespace CyberExtraction.Camp
{
    [Serializable]
    public class CampRecord
    {
        public string Session, Lease, PublicKey;
        public string LeaseToken;
        public long LeaseExpires, LastLoginTicks;
        public List<PvpRewardPair> PvpRewards=new List<PvpRewardPair>();
        // Missing schema fields in legacy JSON must remain at version zero until migration.
        public CampProgress Progress = new CampProgress();
    }

    [Serializable]
    class CampRecords { public const int CurrentVersion=7; public int Version = CurrentVersion; public CampRecord[] Records; }

    // Shared RAM between Windows rooms, with an atomic durable JSON checkpoint.
    // Dois buffers preservam o ultimo estado completo se uma sala cair durante a escrita.
    public sealed class SharedCampMemory : IDisposable
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        const int SlotSize = 2 * 1024 * 1024;
        readonly MemoryMappedFile memory;
        readonly MemoryMappedViewAccessor view;
        readonly Mutex mutex;
        readonly string savePath;

        public SharedCampMemory(string cluster, string directory = null)
        {
            if (string.IsNullOrEmpty(cluster) || cluster.Length > 64) throw new ArgumentException("Cluster invalido.");
            foreach (char c in cluster)
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '-' && c != '_')
                    throw new ArgumentException("Cluster invalido.");
            string name = "Local\\CyberExtractionCamp_" + cluster;
            string folder = directory ?? System.IO.Path.Combine(Application.persistentDataPath,"Camps");
            System.IO.Directory.CreateDirectory(folder);
            savePath=System.IO.Path.Combine(folder,cluster+".json");
            mutex = new Mutex(false, name + "_lock");
            memory = MemoryMappedFile.CreateOrOpen(name, 16 + SlotSize * 2, MemoryMappedFileAccess.ReadWrite);
            view = memory.CreateViewAccessor();
        }

        public T Transaction<T>(Func<Dictionary<string, CampRecord>, T> operation, bool persist = true)
        {
            bool acquired = false;
            try
            {
                try { acquired = mutex.WaitOne(3000); }
                catch (AbandonedMutexException) { acquired = true; }
                if (!acquired) throw new TimeoutException("Acampamento ocupado. Tente novamente.");
                int active = view.ReadInt32(0);
                if (active != 0 && active != 1) throw new InvalidOperationException("Estado compartilhado invalido.");
                long offset = 16 + active * SlotSize;
                int length = view.ReadInt32(offset);
                var records = new Dictionary<string, CampRecord>();
                if (length < 0 || length > SlotSize - 4) throw new InvalidOperationException("Acampamento compartilhado invalido.");
                string migrationJson=null;
                bool preserveBackup=false;
                if (length > 0)
                {
                    var bytes = new byte[length];
                    view.ReadArray(offset + 4, bytes, 0, length);
                    string json=Encoding.UTF8.GetString(bytes);
                    var data = DecodeRecords(json,false,out bool migrated);
                    if(migrated) migrationJson=json;
                    foreach (var record in data.Records) records.Add(record.Session, record);
                }
                else
                {
                    var saved=LoadDiskRecords(out string json,out bool migrated,out preserveBackup);
                    if(saved!=null)
                    {
                        if(migrated) { BackupMigration(json); }
                        foreach(var record in saved.Records) records.Add(record.Session,record);
                        persist=true;
                    }
                }
                if(migrationJson!=null) { BackupMigration(migrationJson); persist=true; }
                T result = operation(records);
                CampProfileMigration.ValidateOwnership(records.Values);
                var output = new CampRecord[records.Count]; records.Values.CopyTo(output, 0);
                var encoded = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new CampRecords { Records = output }));
                if (encoded.Length > SlotSize - 4) throw new InvalidOperationException("Limite de acampamentos desta sessao atingido.");
                // Durable write precedes the shared-memory commit; failure leaves the previous state intact.
                if(persist)
                {
                    string pending=savePath+"."+Guid.NewGuid().ToString("N")+".tmp";
                    try
                    {
                        using(var stream=new System.IO.FileStream(pending,System.IO.FileMode.CreateNew,System.IO.FileAccess.Write,System.IO.FileShare.None)) { stream.Write(encoded,0,encoded.Length); stream.Flush(true); }
                        if(System.IO.File.Exists(savePath)) System.IO.File.Replace(pending,savePath,preserveBackup ? null : savePath+".bak");
                        else System.IO.File.Move(pending,savePath);
                    }
                    finally { if(System.IO.File.Exists(pending)) System.IO.File.Delete(pending); }
                }
                int next = 1 - active;
                long nextOffset = 16 + next * SlotSize;
                view.WriteArray(nextOffset + 4, encoded, 0, encoded.Length);
                view.Write(nextOffset, encoded.Length);
                Thread.MemoryBarrier();
                view.Write(0, next);
                return result;
            }
            finally { if (acquired) mutex.ReleaseMutex(); }
        }
        sealed class UnsupportedCampVersionException : InvalidOperationException
        { public UnsupportedCampVersionException(string message) : base(message) { } }

        static long DocumentVersion(JToken value,string description)
        {
            if(value==null || value.Type!=JTokenType.Integer) throw new InvalidOperationException(description+" ausente ou invalida.");
            try { return value.Value<long>(); }
            catch(OverflowException) { throw new UnsupportedCampVersionException(description+" fora da faixa suportada. Nenhum backup antigo sera restaurado automaticamente."); }
        }
        static CampRecords DecodeRecords(string json,bool fromDisk,out bool migrated)
        {
            // JsonUtility may normalize explicit null arrays to empty arrays and
            // initialize missing objects. Inspect the original structure first.
            var root=JObject.Parse(json,new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error });
            long version=DocumentVersion(root["Version"],"Versao do arquivo");
            if(version>CampRecords.CurrentVersion)
                throw new UnsupportedCampVersionException("Progresso criado por versao mais recente. Atualize o servidor; nenhum backup antigo foi restaurado.");
            if(version<2) throw new InvalidOperationException("Versao do arquivo de progresso invalida.");
            if(!(root["Records"] is JArray entries)) throw new InvalidOperationException("Arquivo de progresso sem lista de registros valida.");
            foreach(var entry in entries)
            {
                if(!(entry is JObject record) || record["Session"]==null || record["Session"].Type!=JTokenType.String ||
                    string.IsNullOrEmpty(record["Session"].Value<string>()) || !(record["Progress"] is JObject profile))
                    throw new InvalidOperationException("Registro de progresso incompleto.");
                var schema=profile["SchemaVersion"];
                if(schema!=null)
                {
                    long schemaVersion=DocumentVersion(schema,"Versao do perfil");
                    if(schemaVersion>CampProfileMigration.CurrentVersion)
                        throw new UnsupportedCampVersionException("Perfil criado por versao mais recente. Nenhum backup antigo foi restaurado.");
                    if(schemaVersion<0) throw new InvalidOperationException("Versao do perfil invalida.");
                }
                ValidateInventoryDocument(profile,schema==null ? 0 : (int)schema.Value<long>());
            }
            // Decode the validated document with explicit historical numeric defaults.
            // JsonUtility can retain constructor-initialized starter ammo in array
            // elements when Data is absent, instead of using the intended zero.
            var data=JsonUtility.FromJson<CampRecords>(root.ToString(Newtonsoft.Json.Formatting.None));
            ValidateFormat(data);
            if(data.Records.Length!=entries.Count) throw new InvalidOperationException("Quantidade de registros inconsistente.");
            migrated=NeedsMigration(data);
            // Validate before recovery clears escrow or replaces the backpack.
            // Missing identities are allowed only for the existing legacy weapon migration.
            CampProfileMigration.ValidateOwnership(data.Records,true);
            var sessions=new HashSet<string>();
            foreach(var record in data.Records)
            {
                if(!sessions.Add(record.Session)) throw new InvalidOperationException("Sessao duplicada no arquivo de progresso.");
                if(fromDisk)
                {
                    record.Lease=null; record.LeaseToken=null; record.LeaseExpires=0; record.Progress.Ensure();
                    if(record.Progress.ExpeditionState!=0) CampRepository.Recover(record.Progress);
                }
                CampProfileMigration.Upgrade(record.Progress);
            }
            CampProfileMigration.ValidateOwnership(data.Records);
            return data;
        }
        static void ValidateInventoryDocument(JObject profile,int schema)
        {
            var state=profile["ExpeditionState"];
            if(state!=null && (state.Type!=JTokenType.Integer || state.Value<long>()<0 || state.Value<long>()>2))
                throw new InvalidOperationException("Estado de expedicao invalido.");
            ValidateSlotsDocument(profile["Chest"],schema<5 ? 9 : CyberExtraction.Items.ItemCatalog.SafeChestSlots);
            ValidateSlotsDocument(profile["Pack"],schema<5 ? 9 : CyberExtraction.Items.ItemCatalog.BackpackSlots);
            if(schema>=5)
            {
                if(!(profile["Merchant"] is JObject merchant) || !(merchant["Offers"] is JArray offers) || offers.Count!=(schema<6 ? 12 : 10) || merchant["Cycle"]?.Type!=JTokenType.String) throw new InvalidOperationException("Estoque persistido ausente.");
                foreach(var token in offers)
                {
                    if(!(token is JObject offer)) throw new InvalidOperationException("Oferta persistida invalida.");
                    foreach(string field in new[] {"Group","Price","Remaining","InitialStock"})
                    {
                        var value=offer[field];
                        if(value==null || value.Type!=JTokenType.Integer || value.Value<long>()<0 || value.Value<long>()>int.MaxValue) throw new InvalidOperationException("Valor de estoque invalido.");
                    }
                    ValidateStackDocument(offer["Item"]);
                }
            }
            if(schema>=2 || profile["Equipment"]!=null && profile["Equipment"].Type!=JTokenType.Null)
                ValidateSlotsDocument(profile["Equipment"],CyberExtraction.Items.EquipmentLoadout.Count);
            foreach(string name in new[] { "Escrow","EscrowEquipment" })
            {
                var container=profile[name];
                if(container!=null && container.Type!=JTokenType.Null)
                    ValidateSlotsDocument(container,name=="Escrow" ? CampProfileMigration.ExpectedEscrowSlots(schema,state==null ? 0 : (int)state.Value<long>(),(container["Slots"] as JArray)?.Count ?? -1) : CyberExtraction.Items.EquipmentLoadout.Count);
            }
            if(!(profile["Ground"] is JArray drops)) throw new InvalidOperationException("Lista de itens do acampamento ausente ou invalida.");
            foreach(var drop in drops)
            {
                if(!(drop is JObject ground) || ground["Id"]?.Type!=JTokenType.String || string.IsNullOrEmpty(ground["Id"].Value<string>()))
                    throw new InvalidOperationException("Item de acampamento invalido.");
                ValidateStackDocument(ground["Stack"]);
            }
        }
        static void ValidateSlotsDocument(JToken container,int count)
        {
            if(!(container is JObject grid) || !(grid["Slots"] is JArray slots) || slots.Count!=count)
                throw new InvalidOperationException("Conteiner de itens ausente ou com tamanho invalido.");
            foreach(var stack in slots) ValidateStackDocument(stack);
        }
        static void ValidateStackDocument(JToken token)
        {
            // Inspect before JsonUtility can default missing fields or narrow an enum to byte.
            if(!(token is JObject stack) || stack["Item"]?.Type!=JTokenType.Integer || stack["Quantity"]?.Type!=JTokenType.Integer)
                throw new InvalidOperationException("Estrutura de item invalida.");
            long item=stack["Item"].Value<long>();
            if(item<0 || item>=CyberExtraction.Items.ItemCatalog.Count)
                throw new InvalidOperationException("Identificador de item inexistente.");
            long quantity=stack["Quantity"].Value<long>();
            if(quantity<0 || quantity>CyberExtraction.Items.ItemCatalog.Limit((CyberExtraction.Items.ItemId)item))
                throw new InvalidOperationException("Quantidade de item fora do limite; perfil preservado para revisao.");
            ValidateStoredInteger(stack["Data"]);
            if(stack["Data"]==null) stack["Data"]=0;
            if(stack["Instance"] is JObject instance)
            {
                ValidateStoredInteger(instance["RecoveryRounds"]);
                if(instance["RecoveryRounds"]==null) instance["RecoveryRounds"]=0;
            }
        }
        static void ValidateStoredInteger(JToken value)
        {
            // Historical fields may be absent (default zero), but must never overflow
            // or change type during JsonUtility deserialization.
            if(value==null) return;
            if(value.Type!=JTokenType.Integer) throw new InvalidOperationException("Metadado numerico de item invalido.");
            long number=value.Value<long>();
            if(number<int.MinValue || number>int.MaxValue)
                throw new InvalidOperationException("Metadado de item fora da faixa suportada.");
        }
        CampRecords ReadCandidate(string path,out string json,out bool migrated)
        {
            if(new System.IO.FileInfo(path).Length>SlotSize-4) throw new InvalidOperationException("Arquivo de progresso excede o limite suportado.");
            json=System.IO.File.ReadAllText(path);
            return DecodeRecords(json,true,out migrated);
        }
        CampRecords LoadDiskRecords(out string json,out bool migrated,out bool recoveredFromBackup)
        {
            recoveredFromBackup=false;
            Exception primaryError;
            try { return ReadCandidate(savePath,out json,out migrated); }
            catch(UnsupportedCampVersionException) { throw; }
            catch(Exception error) { primaryError=error; }

            // Preserve rejected bytes before any replacement can overwrite .bak.
            bool missingPrimary=primaryError is System.IO.FileNotFoundException;
            if(!missingPrimary) PreserveCandidate(savePath,"rejected");
            CampRecords recovered;
            try { recovered=ReadCandidate(savePath+".bak",out json,out migrated); }
            catch(UnsupportedCampVersionException) { throw; }
            catch(System.IO.FileNotFoundException) when(missingPrimary)
            { json=null; migrated=false; return null; } // A genuinely new store, not a failed read.
            catch(Exception backupError)
            { throw new InvalidOperationException("Principal e backup indisponiveis ou invalidos. Nenhum progresso vazio foi gravado.",new AggregateException(primaryError,backupError)); }

            PreserveCandidate(savePath+".bak","recovered-backup");
            recoveredFromBackup=true;
            Debug.LogWarning("Acampamento recuperado do backup; documentos originais preservados. Motivo do principal: "+primaryError.GetType().Name);
            return recovered;
        }
        void PreserveCandidate(string source,string label)
        {
            // Content-addressed archives avoid a new copy on every failed retry.
            using(var input=new System.IO.FileStream(source,System.IO.FileMode.Open,System.IO.FileAccess.Read,System.IO.FileShare.Read))
            using(var sha=System.Security.Cryptography.SHA256.Create())
            {
                var hash=sha.ComputeHash(input);
                string suffix=BitConverter.ToString(hash,0,16).Replace("-","").ToLowerInvariant();
                string archive=savePath+"."+label+"-"+suffix+".json";
                if(System.IO.File.Exists(archive))
                {
                    using(var existing=System.IO.File.OpenRead(archive))
                        if(Convert.ToBase64String(sha.ComputeHash(existing))!=Convert.ToBase64String(hash))
                            throw new System.IO.IOException("Arquivo de preservacao existente nao corresponde ao progresso original.");
                    return;
                }
                string pending=archive+"."+Guid.NewGuid().ToString("N")+".tmp";
                try
                {
                    input.Position=0;
                    using(var output=new System.IO.FileStream(pending,System.IO.FileMode.CreateNew,System.IO.FileAccess.Write,System.IO.FileShare.None))
                    { input.CopyTo(output); output.Flush(true); }
                    System.IO.File.Move(pending,archive);
                }
                finally { if(System.IO.File.Exists(pending)) System.IO.File.Delete(pending); }
            }
        }
        static void ValidateFormat(CampRecords data)
        {
            if(data==null || data.Records==null || data.Version<2 || data.Version>CampRecords.CurrentVersion)
                throw new InvalidOperationException("Arquivo de progresso invalido ou de versao nao suportada.");
            foreach(var record in data.Records)
                if(record==null || string.IsNullOrEmpty(record.Session) || record.Progress==null)
                    throw new InvalidOperationException("Registro de progresso invalido.");
        }
        static bool NeedsMigration(CampRecords data)
        {
            bool needed=data.Version<CampRecords.CurrentVersion;
            foreach(var record in data.Records) needed|=CampProfileMigration.NeedsUpgrade(record.Progress);
            return needed;
        }
        void BackupMigration(string json)
        {
            string path=savePath+".pre-v"+CampRecords.CurrentVersion+"-"+Guid.NewGuid().ToString("N")+".json";
            byte[] bytes=Encoding.UTF8.GetBytes(json);
            using(var stream=new System.IO.FileStream(path,System.IO.FileMode.CreateNew,System.IO.FileAccess.Write,System.IO.FileShare.None))
            { stream.Write(bytes,0,bytes.Length); stream.Flush(true); }
        }
        public void Dispose() { view.Dispose(); memory.Dispose(); mutex.Dispose(); }
#else
        public SharedCampMemory(string cluster, string directory = null) { throw new PlatformNotSupportedException("As salas dedicadas usam Windows."); }
        public T Transaction<T>(Func<Dictionary<string, CampRecord>, T> operation, bool persist = true) { throw new PlatformNotSupportedException(); }
        public void Dispose() { }
#endif
    }
}

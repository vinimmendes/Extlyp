using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.IO.MemoryMappedFiles;
using System.Threading;
#endif

namespace CyberExtraction.Camp
{
    [Serializable]
    public class CampRecord
    {
        public string Session, Lease;
        public long LeaseExpires;
        public CampProgress Progress = new CampProgress();
    }

    [Serializable]
    class CampRecords { public CampRecord[] Records; }

    // RAM compartilhada entre as salas Windows. Nada e salvo em banco/arquivo.
    // Dois buffers preservam o ultimo estado completo se uma sala cair durante a escrita.
    public sealed class SharedCampMemory
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        const int SlotSize = 2 * 1024 * 1024;
        readonly MemoryMappedFile memory;
        readonly MemoryMappedViewAccessor view;
        readonly Mutex mutex;

        public SharedCampMemory(string cluster)
        {
            if (string.IsNullOrEmpty(cluster) || cluster.Length > 64) throw new ArgumentException("Cluster invalido.");
            foreach (char c in cluster)
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '-' && c != '_')
                    throw new ArgumentException("Cluster invalido.");
            string name = "Local\\CyberExtractionCamp_" + cluster;
            mutex = new Mutex(false, name + "_lock");
            memory = MemoryMappedFile.CreateOrOpen(name, 16 + SlotSize * 2, MemoryMappedFileAccess.ReadWrite);
            view = memory.CreateViewAccessor();
        }

        public T Transaction<T>(Func<Dictionary<string, CampRecord>, T> operation)
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
                if (length > 0)
                {
                    var bytes = new byte[length];
                    view.ReadArray(offset + 4, bytes, 0, length);
                    var data = JsonUtility.FromJson<CampRecords>(Encoding.UTF8.GetString(bytes));
                    if (data == null || data.Records == null) throw new InvalidOperationException("Acampamento compartilhado invalido.");
                    foreach (var record in data.Records) records.Add(record.Session, record);
                }
                T result = operation(records);
                var output = new CampRecord[records.Count]; records.Values.CopyTo(output, 0);
                var encoded = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new CampRecords { Records = output }));
                if (encoded.Length > SlotSize - 4) throw new InvalidOperationException("Limite de acampamentos desta sessao atingido.");
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
#else
        public SharedCampMemory(string cluster) { throw new PlatformNotSupportedException("As salas dedicadas usam Windows."); }
        public T Transaction<T>(Func<Dictionary<string, CampRecord>, T> operation) { throw new PlatformNotSupportedException(); }
#endif
    }
}

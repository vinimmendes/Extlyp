using System;
using System.Collections.Generic;

namespace CyberExtraction.Camp
{
    [Serializable]
    public class CampJoinRequest
    {
        public string Session;
        public bool RepairPier, Fish;
        public int BringBandages;
        public CampAction[] Actions;
    }

    // Prototipo sem contas: as salas conservam o acampamento na RAM compartilhada.
    public static class CampRepository
    {
        static readonly Dictionary<string, CampProgress> profiles = new Dictionary<string, CampProgress>();
        static readonly Dictionary<string, CampRecord> localRecords = new Dictionary<string, CampRecord>();
        static SharedCampMemory shared;
        public static void EnableShared(string cluster) { shared = new SharedCampMemory(cluster); }

        public static T Edit<T>(string session, Func<CampProgress, T> operation)
        {
            if (shared == null) return operation(Get(session));
            return shared.Transaction(records => operation(Record(records, session).Progress));
        }

        static CampRecord Record(Dictionary<string, CampRecord> records, string session)
        {
            if (!records.TryGetValue(session, out var record))
                records[session] = record = new CampRecord { Session = session };
            return record;
        }

        static T LeaseTransaction<T>(Func<Dictionary<string, CampRecord>, T> operation) =>
            shared != null ? shared.Transaction(operation) : operation(localRecords);

        public static bool Claim(string session, string owner)
        {
            return LeaseTransaction(records =>
            {
                var record = Record(records, session);
                long now = DateTime.UtcNow.Ticks;
                if (!string.IsNullOrEmpty(record.Lease) && record.Lease != owner && record.LeaseExpires > now) return false;
                record.Lease = owner; record.LeaseExpires = now + TimeSpan.FromSeconds(90).Ticks;
                return true;
            });
        }

        public static void Refresh(string session, string owner)
        {
            LeaseTransaction(records =>
            {
                var record = Record(records, session);
                if (record.Lease == owner) record.LeaseExpires = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(90).Ticks;
                return 0;
            });
        }

        public static void Release(string session, string owner)
        {
            LeaseTransaction(records =>
            {
                var record = Record(records, session);
                if (record.Lease == owner) { record.Lease = null; record.LeaseExpires = 0; }
                return 0;
            });
        }

        public static CampProgress Get(string session)
        {
            if (shared != null) return shared.Transaction(records => Record(records, session).Progress.Copy());
            if (!profiles.TryGetValue(session, out var profile)) profiles[session] = profile = new CampProgress();
            return profile;
        }

        public static void Apply(CampJoinRequest request)
        {
            Edit(request.Session, profile =>
            {
                if (request.RepairPier) profile.RepairPier();
                if (request.Actions != null)
                    for (int i = 0; i < Math.Min(64, request.Actions.Length); i++) profile.Apply(request.Actions[i]);
                return 0;
            });
        }
    }

    public static class CampSession
    {
        public static readonly string Identity = LoadIdentity();
        public static CampProgress Progress = new CampProgress();
        public static int BringBandages;
        public static bool RepairPending, FishPending;
        public static readonly List<CampAction> PendingActions = new List<CampAction>();
        public static bool Act(CampAction action)
        {
            if (PendingActions.Count >= 64 || !Progress.Apply(action)) return false;
            PendingActions.Add(action); return true;
        }

        static string LoadIdentity()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-smoke-test") >= 0 || Array.IndexOf(args, "-items-smoke-test") >= 0 ||
                Array.IndexOf(args, "-camp-preview") >= 0) return Guid.NewGuid().ToString("N");
            string identity = UnityEngine.PlayerPrefs.GetString("camp_session", "");
            if (!Guid.TryParseExact(identity, "N", out _))
            {
                identity = Guid.NewGuid().ToString("N");
                UnityEngine.PlayerPrefs.SetString("camp_session", identity);
                UnityEngine.PlayerPrefs.Save();
            }
            return identity;
        }

        public static CampJoinRequest Request() => new CampJoinRequest
        { Session = Identity, RepairPier = RepairPending, Fish = false, BringBandages = BringBandages, Actions = PendingActions.ToArray() };

        public static void Accept(string json)
        {
            var snapshot = UnityEngine.JsonUtility.FromJson<CampProgress>(json);
            if (snapshot == null) return;
            snapshot.Ensure(); Progress = snapshot; PendingActions.Clear(); RepairPending = FishPending = false;
        }

        public static void Accept(int wood, int nails, int bandages, bool pier, bool food)
        {
            Progress = new CampProgress { Wood = wood, Nails = nails, Bandages = bandages, PierRepaired = pier, WellFed = food };
            RepairPending = FishPending = false;
            BringBandages = Math.Min(BringBandages, bandages);
        }
    }
}


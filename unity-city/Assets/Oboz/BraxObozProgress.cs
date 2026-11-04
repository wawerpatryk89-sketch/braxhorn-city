using System;
using UnityEngine;

// A foreign/old schema stays untouched until its actual migration is known.
public sealed class BraxObozProgress : MonoBehaviour
{
    public const string Key = "braxhorn_oboz_progress_v1";
    public const string Schema = "braxhorn-oboz/recovery-2";
    [Serializable] public sealed class SaveData
    {
        public string schemaId = Schema;
        public int version = 2, trainingHits, wonSparrings, lostSparrings;
        public float campMinutes = 16 * 60;
    }
    public SaveData Data { get; private set; } = new SaveData();
    public bool ReadOnly { get; private set; }
    bool dirty;
    float nextSave;

    void Awake()
    {
        if (!PlayerPrefs.HasKey(Key)) return;
        string raw = PlayerPrefs.GetString(Key, "");
        try
        {
            // JsonUtility field initializers must not make an unknown payload
            // look like our own save. Require an explicit schema in the text.
            if (string.IsNullOrEmpty(raw) || !raw.Contains("\"schemaId\""))
                throw new FormatException("Brak identyfikatora schematu");
            var save = JsonUtility.FromJson<SaveData>(raw);
            if (save == null || save.schemaId != Schema || save.version != 2 ||
                save.trainingHits < 0 || save.wonSparrings < 0 || save.lostSparrings < 0 ||
                float.IsNaN(save.campMinutes) || float.IsInfinity(save.campMinutes))
                throw new FormatException("Inny lub uszkodzony schemat");
            save.campMinutes = Mathf.Repeat(save.campMinutes, 1440);
            Data = save;
        }
        catch (Exception e)
        {
            ReadOnly = true;
            Debug.LogWarning("BRAXHORN_OBOZ zapis zachowany bez nadpisania; wymagana migracja: " + e.Message, this);
        }
    }
    public void SetTime(float minutes) { Data.campMinutes = Mathf.Repeat(minutes, 1440); dirty = true; }
    public void TrainingHit() { if (Data.trainingHits < int.MaxValue) Data.trainingHits++; dirty = true; }
    public void FinishSparring(bool won)
    {
        if (won && Data.wonSparrings < int.MaxValue) Data.wonSparrings++;
        else if (!won && Data.lostSparrings < int.MaxValue) Data.lostSparrings++;
        dirty = true; Save();
    }
    void Update() { if (dirty && Time.unscaledTime >= nextSave) { nextSave = Time.unscaledTime + 30; Save(); } }
    public void Save()
    {
        if (!dirty || ReadOnly) return;
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data)); PlayerPrefs.Save(); dirty = false;
    }
    void OnApplicationPause(bool paused) { if (paused) Save(); }
    void OnApplicationQuit() { Save(); }
    void OnDisable() { Save(); }
}

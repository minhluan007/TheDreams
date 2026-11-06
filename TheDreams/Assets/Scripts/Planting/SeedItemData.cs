using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class PlantGrowthStage
{
    [Tooltip("Sprite o giai nay.")]
    public Sprite sprite;

    [Min(0)]
    [Tooltip("So ngay ke tu luc giao hat de hien sprite nay. Stage dau thuong = 0.")]
    public int daysFromPlanting;
}

[CreateAssetMenu(
    fileName = "Seed_",
    menuName = "The Dreams/Inventory/Seed"
)]
public class SeedItemData : ItemData
{
    [Tooltip("Item nay chi dung de trong cay (khong phai hang ban thong thuong).")]
    public bool plantingOnly = true;

    [Tooltip("Moi dong = 1 sprite + so ngay ke tu luc giao hat.")]
    public PlantGrowthStage[] stages =
    {
        new PlantGrowthStage { daysFromPlanting = 0 }
    };

    [Tooltip("Offset sprite cay so voi tam dat/chau.")]
    public Vector2 plantOffset = new Vector2(0f, 0.55f);

    [Tooltip("Scale sprite cay trong chau/dat. Tang neu cay qua nho.")]
    [Min(0.1f)]
    public float plantScale = 2f;

    // Du lieu cu (neu asset chua migrate).
    [HideInInspector] public Sprite[] growthStages;
    [HideInInspector] public int[] daysPerStage;
    [HideInInspector] public int daysPerStageDefault = 1;

    public int StageCount => stages != null ? stages.Length : 0;

    public int ResolveStage(int daysGrown)
    {
        EnsureStages();
        int count = StageCount;
        if (count <= 0)
            return 0;

        int grown = Mathf.Max(0, daysGrown);
        int best = 0;
        int bestDays = int.MinValue;
        for (int i = 0; i < count; i++)
        {
            PlantGrowthStage stage = stages[i];
            if (stage == null)
                continue;

            int need = Mathf.Max(0, stage.daysFromPlanting);
            if (grown < need)
                continue;

            if (need < bestDays)
                continue;

            bestDays = need;
            best = i;
        }

        return Mathf.Clamp(best, 0, count - 1);
    }

    public Sprite StageSprite(int stageIndex)
    {
        EnsureStages();
        if (stages == null || stages.Length == 0)
            return icon;

        int index = Mathf.Clamp(stageIndex, 0, stages.Length - 1);
        PlantGrowthStage stage = stages[index];
        if (stage != null && stage.sprite != null)
            return stage.sprite;

        return icon;
    }

    public void EnsureStages()
    {
        if (stages != null && stages.Length > 0)
            return;

        MigrateLegacyStages();
    }

    public void MigrateLegacyStages()
    {
        if (growthStages == null || growthStages.Length == 0)
        {
            if (stages == null || stages.Length == 0)
                stages = new[] { new PlantGrowthStage { sprite = icon, daysFromPlanting = 0 } };
            return;
        }

        stages = new PlantGrowthStage[growthStages.Length];
        int cumulative = 0;
        for (int i = 0; i < growthStages.Length; i++)
        {
            stages[i] = new PlantGrowthStage
            {
                sprite = growthStages[i],
                daysFromPlanting = cumulative
            };

            if (i >= growthStages.Length - 1)
                continue;

            int gap = daysPerStage != null && i < daysPerStage.Length
                ? Mathf.Max(1, daysPerStage[i])
                : Mathf.Max(1, daysPerStageDefault);
            cumulative += gap;
        }
    }

    public void SortStagesByDay()
    {
        if (stages == null || stages.Length <= 1)
            return;

        Array.Sort(stages, (a, b) =>
        {
            int dayA = a != null ? a.daysFromPlanting : 0;
            int dayB = b != null ? b.daysFromPlanting : 0;
            return dayA.CompareTo(dayB);
        });
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (stages == null || stages.Length == 0)
            MigrateLegacyStages();
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(SeedItemData))]
public class SeedItemDataEditor : Editor
{
    SerializedProperty plantingOnly;
    SerializedProperty stages;
    SerializedProperty plantOffset;
    SerializedProperty plantScale;

    void OnEnable()
    {
        plantingOnly = serializedObject.FindProperty("plantingOnly");
        stages = serializedObject.FindProperty("stages");
        plantOffset = serializedObject.FindProperty("plantOffset");
        plantScale = serializedObject.FindProperty("plantScale");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        ((SeedItemData)target).EnsureStages();
        serializedObject.Update();

        DrawBaseItemFields();
        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(plantingOnly);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Tang truong (ngay ke tu giao hat)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Moi dong: Sprite + so ngay ke tu luc giao hat.\n" +
            "Vi du: ngay 0 = moi trong, ngay 2 = cay non, ngay 5 = truong thanh.\n" +
            "Game chon sprite co \"Ngay\" lon nhat ma van <= so ngay da qua.",
            MessageType.Info);

        DrawStagesTable();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Them giai"))
        {
            int index = stages.arraySize;
            stages.InsertArrayElementAtIndex(index);
            SerializedProperty row = stages.GetArrayElementAtIndex(index);
            SerializedProperty days = row.FindPropertyRelative("daysFromPlanting");
            SerializedProperty sprite = row.FindPropertyRelative("sprite");
            sprite.objectReferenceValue = null;
            int prev = 0;
            if (index > 0)
            {
                SerializedProperty prevDays = stages.GetArrayElementAtIndex(index - 1)
                    .FindPropertyRelative("daysFromPlanting");
                prev = prevDays.intValue + 1;
            }

            days.intValue = prev;
        }

        if (GUILayout.Button("Sap xep theo ngay"))
        {
            serializedObject.ApplyModifiedProperties();
            SeedItemData seed = (SeedItemData)target;
            Undo.RecordObject(seed, "Sort growth stages");
            seed.SortStagesByDay();
            EditorUtility.SetDirty(seed);
            serializedObject.Update();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8f);
        EditorGUILayout.PropertyField(plantOffset);
        EditorGUILayout.PropertyField(plantScale);

        if (stages.arraySize > 0)
        {
            int last = stages.GetArrayElementAtIndex(stages.arraySize - 1)
                .FindPropertyRelative("daysFromPlanting").intValue;
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Truong thanh sau: " + last + " ngay (ke tu giao hat).",
                EditorStyles.boldLabel);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawBaseItemFields()
    {
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (iterator.name == "m_Script" ||
                iterator.name == "plantingOnly" ||
                iterator.name == "stages" ||
                iterator.name == "plantOffset" ||
                iterator.name == "plantScale" ||
                iterator.name == "growthStages" ||
                iterator.name == "daysPerStage" ||
                iterator.name == "daysPerStageDefault")
                continue;

            EditorGUILayout.PropertyField(iterator, true);
        }
    }

    void DrawStagesTable()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("#", GUILayout.Width(24f));
        GUILayout.Label("Sprite", GUILayout.Width(70f));
        GUILayout.Label("Ngay ke tu giao hat", GUILayout.MinWidth(120f));
        GUILayout.Label("", GUILayout.Width(24f));
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < stages.arraySize; i++)
        {
            SerializedProperty row = stages.GetArrayElementAtIndex(i);
            SerializedProperty sprite = row.FindPropertyRelative("sprite");
            SerializedProperty days = row.FindPropertyRelative("daysFromPlanting");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(i.ToString(), GUILayout.Width(24f));
            sprite.objectReferenceValue = EditorGUILayout.ObjectField(
                sprite.objectReferenceValue,
                typeof(Sprite),
                false,
                GUILayout.Width(70f),
                GUILayout.Height(54f));
            days.intValue = Mathf.Max(0, EditorGUILayout.IntField(days.intValue));

            using (new EditorGUI.DisabledScope(stages.arraySize <= 1))
            {
                if (GUILayout.Button("X", GUILayout.Width(24f)))
                {
                    stages.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
            }

            EditorGUILayout.EndHorizontal();

            string note = days.intValue <= 0
                ? "Moi giao hat"
                : ("Hien khi da qua >= " + days.intValue + " ngay");
            EditorGUILayout.LabelField(note, EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }
}
#endif

using UnityEngine;

public class PlantingHierarchyGuide : MonoBehaviour
{
    [TextArea(20, 40)]
    [SerializeField]
    string hierarchyNotes =
@"TRONG CAY / CHAU (Town demo)

------ Item Data ------
Create > The Dreams/Inventory/Seed  -> SeedItemData (chi de trong)
Create > The Dreams/Inventory/Pot   -> PotItemData (chi de dat chau)
Item_EmptyPot: PotItemData + gan potPrefab = Prefab PlantBed_Pot
Item_TomatoSeed: SeedItemData
  stages[] = Sprite + daysFromPlanting (ngay ke tu giao hat)
  Vi du: ngay 0, 1, 3, 5, 8 = TomatoPlant_0..4

------ Prefab PlantBed ------
PlantBed_Soil / PlantBed_Pot
  SpriteRenderer (dat/chau)
  BoxCollider2D IsTrigger ~ 1.2 x 1.2
  PlantBed.cs
    kind = Soil | Pot
    bedId (unique trong scene)
  PlantVisual (child, tu tao neu thieu)

------ Snap dat chau ------
PutSnap_1  [Empty + PutSnapPoint]
  accepts = Pot
  Gizmo hinh tron xanh = trong

------ Player ------
ItemPut + PlayerApproach (PlantingDemoSetup / EnsureOnPlayer tu them)

Flow chau:
1. Mo Inv, click Empty Pot -> cac PutSnap trong sang len
2. Detail hien nut ""Dat chau""
3. Bam Dat chau -> Inv an, icon chau theo chuot
4. Click vao snap sang -> player di toi va dat chau
   Esc huy

Flow hat:
1. Click Tomato Seed -> cac PlantBed trong (chua co cay) sang
2. Nut ""Giao hat"" -> Inv an, hat theo chuot
3. Click chau/dat trong -> player di toi va giao hat

------ Demo Town ------
Object: ------ Planting Demo ------
  PlantingHierarchyGuide
  PlantingDemoSetup  (Play mode spawn snap/soil/pickup neu thieu)

------ Choi thu ------
1. Nhat Empty Pot / Tomato Seed (E)
2. Mo Inv, chon chau -> thay snap sang -> Dat chau -> click snap
3. Chon Seed -> thay bed sang -> Giao hat -> click chau/dat
4. Bam N (demo) de +1 ngay, xem cay lon
5. Save/Load de kiem tra PlantWorld + lich

------ LifeTime ------
Ngay / Thang / Nam thu 1, 2, ... (khong dung nam duong lich)
Het 24:00 -> sang ngay moi. Cay tinh theo AbsoluteDay.
UI LifeTimePanel: DayText, MonthText, YearText, TimeText.";
}

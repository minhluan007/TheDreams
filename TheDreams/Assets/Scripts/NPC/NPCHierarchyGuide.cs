using UnityEngine;

public class NPCHierarchyGuide : MonoBehaviour
{
    [TextArea(16, 32)]
    [SerializeField]
    string hierarchyNotes =
@"HIERARCHY NPC (chua co art)

------ NPC ------
  NPC                    [Prefab NPC / Empty]
    NPC.cs
    NPCMove.cs
    NPCMapTraveler.cs
    Rigidbody2D (Kinematic, Freeze Rotation)
    BoxCollider2D (khong phai Trigger, size ~ 1 x 0.4)
    Sprite_NPC           [Empty child]
      SpriteRenderer     (gan sprite sau)
      Animator           (Run / Idle giong Player)
      SortY
      NPCPlaceholderVisual  (mau tam khi chua co sprite)

  NPC_Route_BaseStore    [Empty + NPCRoute]  sceneName = BaseStore
    WP_Street_1
    WP_Street_2
    WP_Door              (gan Exit_ToStore)

------ NPC ------ (scene InStore)
  NPC_Route_InStore      [Empty + NPCRoute]  sceneName = InStore
    WP_Entrance          (gan Spawn_FromStreet)
    WP_Shelf

Giu nguyen:
  ------ Spawn Points ------
    Exit_ToStore / Exit_ToStreet  (MapExit + BoxCollider2D Is Trigger)
    Spawn_FromStreet / Spawn_FromStore

Lich NPC nam o Assets/NPC/DataSO/Resources/NPCSchedule.
Moi dong text: id | prefab | map | spawn | di | destroy | xuat hien | roi di
Ten diem la ten object trong scene. Gio de trong khi chua co dong ho.
Sau khi sua text, bam Nap text vao bang. Toi diem destroy thi Destroy.
Dong ho sau nay goi NPCManager.ReportHour.";
}

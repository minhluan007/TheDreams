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
Moi dong la mot chang, khong phai mot NPC moi:
id | prefab | map | spawn | di | destroy | xuat hien | roi di | hanh vi
Cung id, nhieu dong, dung thu tu = mot ngay.
Hanh vi: di, xem, chon, mua, dung, ra.
Dong ho bat dau luc dayStartHour. NPC chua toi gio thi chua co mat.
Doi map khong reset: NPC tiep tuc dung chang dang lam.
Xong chang nay thi sang chang sau (vao cua hang, roi ra).
Neu ke co hang, chon se lay 1 mon. Ke trong thi xem roi bo di.";
}

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
    WP_CheckoutCounter   (NPC dung day de thanh toan)
  Table_Checkout         (NPC dat mon len ban nay)

Giu nguyen:
  ------ Spawn Points ------
    Exit_ToStore / Exit_ToStreet  (MapExit + BoxCollider2D Is Trigger)
    Spawn_FromStreet / Spawn_FromStore

Lich NPC nam o Assets/NPC/DataSO/Resources/NPCSchedule.
Moi dong la mot chang, khong phai mot NPC moi:
id | prefab | map | spawn | di | destroy | xuat hien | roi di | hanh vi
Cung id, nhieu dong, dung thu tu = mot ngay.
Hanh vi: di, xem, chon, mua, doi, tra, dung, ra.
mua lay mon o ke, chua cong coin.
doi toi WP_CheckoutCounter, dat mon len Table_Checkout, dung 3 giay.
tra moi cong coin. Sang action khac thi lay mon khoi Table_Checkout. Scene player bam ban se lam sau.
Khong co mua/doi/tra, hoac ke trong, thi khach chi xem roi ve.
Dong ho la LifeTime. NPCSchedule doc gio xuat hien / roi di tu LifeTime. NPC chua toi gio thi chua co mat.
Doi map khong reset: NPC tiep tuc dung chang dang lam.
Xong chang nay thi sang chang sau (vao cua hang, roi ra).";
}

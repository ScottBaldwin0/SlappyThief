using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;
using UnityEngine.AI;

public class ShopperBehaviour : MonoBehaviour
{
    public List<ShopItem> ShopperCart;
    [SerializeField]
    float BaseMood;
    [SerializeField]
    ShopItemTypes.SHOPITEMTYPE RequestedItemType;

    [SerializeField] private GameObject thoughtBubble = null; // Added for bubble instantiation -- SB
    
    float Mood;
    [SerializeField]
    float BaseRequestTime;
    float Timer;
    [SerializeField]
    int MaxCartSize;
    int TargetItems;
    public bool isInQueue;
    public bool isPendingItemRequest;
    [SerializeField]
    float MoodDelta;
    BoxCollider PickupBox;
    ShopInfo ShopInfo;

    Transform ShopperMovement;

    [SerializeField]
    float SlapVelocity;

    [SerializeField]
    Vector3 CartItemOffset;

    [SerializeField]
    Vector3 PickupRange;

    InteractionBehaviour ib;
    CapsuleCollider ShopperCollider;


    [SerializeField]
    Vector3 PickupBoxOffset;

    [SerializeField]
    float BaseShakeTimer;
    float ShakeTimer;

    NavMeshAgent nma;
    

    
    private void Start()
    { 

        if ((ib = GetComponent<InteractionBehaviour>())==null)
        {
            ib = gameObject.AddComponent<InteractionBehaviour>();
        }

        if ((ShopperCollider = GetComponent<CapsuleCollider>()) == null)
        {
            ShopperCollider = gameObject.AddComponent<CapsuleCollider>();
        }

        ShopperCollider.isTrigger = false;
        ShopperCollider.radius = 0.125f;
        ShopperCollider.height = 0.5f;

        RequestedItemType = ShopItemTypes.SHOPITEMTYPE.UNDEFINED;
        ShopperCart = new List<ShopItem>();
        BaseRequestTime *= Random.Range(1, 5);
        Timer = BaseRequestTime;
        BaseMood *= Random.Range(1, 5);
        Mood = BaseMood;
        TargetItems = Random.Range(1, MaxCartSize);
        isInQueue = false;
        
        ib.OnContactBegin += OnSlap;
        ib.OnGraspBegin += OnGraspBegin;
        ib.OnGraspEnd += OnGraspEnd;
        ib.OnGraspStay += OnShake;

        ShakeTimer = BaseShakeTimer;
        ShopperMovement = GetComponentInChildren<SkinnedMeshRenderer>().rootBone;
        PickupBox = gameObject.AddComponent<BoxCollider>();
        PickupBox.isTrigger = true;
        PickupBox.center = ShopperCollider.center = ShopperMovement.localPosition + PickupBoxOffset;
        PickupBox.size = PickupRange;

        nma = gameObject.GetComponent<NavMeshAgent>();
        ShopInfo = FindObjectOfType<ShopInfo>();
        
    }
    void OnGraspBegin()
    {
        nma.enabled = false;
        //navmesh does not like being taken hostage 
    }

    void OnGraspEnd()
    {
        nma.enabled = true;
    }

    void OnSlap()
    {
        if(ib.closestHoveringController != null && ib.closestHoveringController.velocity.magnitude > SlapVelocity)
        {
            DropRandomItem();
        }
    }

    void OnShake()
    {
        if(ib.closestHoveringController != null && ib.graspingController.velocity.magnitude > SlapVelocity && ShakeTimer < 0)
        {
            DropRandomItem();
            ShakeTimer = BaseShakeTimer;
        }
        else
        {
            ShakeTimer -= Time.deltaTime;
        }
    }


    void DropRandomItem()
    {
        if (ShopperCart.Count > 0)
        {
            RemoveItemFromCart(ShopperCart[Random.Range(0, ShopperCart.Count)]);
            Debug.Log(name + " Dropped an item!");
        }
    }

    private void Update()
    {
        HandleItemRequests();
        if(RequestedItemType != ShopItemTypes.SHOPITEMTYPE.UNDEFINED)
        {
            //TODO: draw speech bubble 
        }
        else
        {
            //TODO: hide speechbubble
        }

        if (ShopperCart.Count > 0)
        {
            RenderCart();
        }
    }

    void RenderCart()
    {
        for (int i = 0; i < ShopperCart.Count; ++i)
        {
            ShopItem s = ShopperCart[i];
            AnchorableBehaviour a; 
            if ((a = s.GetComponent<AnchorableBehaviour>()).isAttached)
            {
                a.Detach();
            }
            Transform t = s.gameObject.transform;
            t.position =  Vector3.Lerp(t.transform.position,transform.position +  (t.forward *  ((i + 1) * CartItemOffset.x)) + (t.up * CartItemOffset.y)  + (t.right * CartItemOffset.z),0.8f);
            s.gameObject.transform.rotation = s.GetBaseRotation();
        }
    }


    void HandleItemRequests()
    {
        if (!isInQueue && Timer <= 0 && ShopperCart.Count < TargetItems && RequestedItemType == ShopItemTypes.SHOPITEMTYPE.UNDEFINED)
        {
            isPendingItemRequest = true; //request an item 
            Timer = BaseRequestTime;
        }
        else
        {
            Timer -= Time.deltaTime;
            if (RequestedItemType != ShopItemTypes.SHOPITEMTYPE.UNDEFINED || (isInQueue && ShopperCart.Count != 0))
            {
                Mood -= Time.deltaTime;
            }
        }
    }

    public void AddItemToCart(ShopItem s)
    {
        ShopperCart.Add(s);
        ShopInfo.RemoveShopItem(s);
        RequestedItemType = ShopItemTypes.SHOPITEMTYPE.UNDEFINED;
        Rigidbody r = s.GetComponent<Rigidbody>();
        r.isKinematic = true;
        r.useGravity = false;
        r.velocity = Vector3.zero;
        s.GetComponent<AnchorableBehaviour>().Detach();

    }

    public void RemoveItemFromCart(ShopItem s)
    {
        ShopperCart.Remove(s);
        Rigidbody r = s.GetComponent<Rigidbody>();
        r.isKinematic = false;
        r.useGravity = true;
        r.velocity = Vector3.zero;

    }

    public void RequestItem(ShopItemTypes.SHOPITEMTYPE s)
    {
        RequestedItemType = s;
        Debug.Log(name + " requesting " + s.ToString());
        isPendingItemRequest = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        ShopItem s;
        if ((s = other.gameObject.GetComponentInParent<ShopItem>()) != null && s.ShopItemType == RequestedItemType && ShopInfo.AvailableItemsByType[(int)RequestedItemType].Contains(s) && !ShopperCart.Contains(s))
        {
            AnchorableBehaviour ab;
            if ((ab = other.gameObject.GetComponentInParent<AnchorableBehaviour>()) != null && ab.isAttached)
            {
                ab.Detach(); //prevents an object from being both in cart and in inventory
            }
            AddItemToCart(s);          
        }
    }
}

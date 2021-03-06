using Assets.Scripts.Characters.Titan;
using Assets.Scripts.Gamemode.Options;
using System.Collections;
using UnityEngine;

public class TriggerColliderWeapon : MonoBehaviour
{
    public Equipment Equipment { get; set; }

    public bool active_me;
    public GameObject currentCamera;
    public ArrayList currentHits = new ArrayList();
    public ArrayList currentHitsII = new ArrayList();
    public AudioSource meatDie;
    public int myTeam = 1;
    public float scoreMulti = 1f;

    private bool checkIfBehind(GameObject titan)
    {
        if (titan.transform.Find("Amarture/Core/Controller_Body/hip/spine/chest/neck/head") != null)
        {
            Transform transform = titan.transform.Find("Amarture/Core/Controller_Body/hip/spine/chest/neck/head");
            Vector3 to = base.transform.position - transform.transform.position;
            return (Vector3.Angle(-transform.transform.forward, to) < 70f);
        }
        else if (titan.transform.Find("BodyPivot/HeadPos") != null)// dummy titan
        {
            Transform transform = titan.transform.Find("BodyPivot/HeadPos");
            Vector3 to = base.transform.position - transform.transform.position;
            return (Vector3.Angle(-transform.transform.forward, to) < 70f);
        }
        return false;
    }

    public void DummyNapeHit(DummyTitan titan)
    {
        Vector3 vector3 = this.currentCamera.GetComponent<IN_GAME_MAIN_CAMERA>().main_object.GetComponent<Rigidbody>().velocity;
        int num2 = (int)((vector3.magnitude * 10f) * this.scoreMulti);
        num2 = Mathf.Max(10, num2);

        titan.photonView.RPC("GetHit", PhotonTargets.All, new object[] { num2 });
        GameObject.Find("MultiplayerManager").GetComponent<FengGameManagerMKII>().netShowDamage(num2);
    }

    public void clearHits()
    {
        this.currentHitsII = new ArrayList();
        this.currentHits = new ArrayList();
    }

    private void napeMeat(Vector3 vkill, Transform titan)
    {
        Transform transform = titan.transform.Find("Amarture/Core/Controller_Body/hip/spine/chest/neck");
        GameObject obj2 = (GameObject) UnityEngine.Object.Instantiate(Resources.Load("titanNapeMeat"), transform.position, transform.rotation);
        obj2.transform.localScale = titan.localScale;
        obj2.GetComponent<Rigidbody>().AddForce((Vector3) (vkill.normalized * 15f), ForceMode.Impulse);
        obj2.GetComponent<Rigidbody>().AddForce((Vector3) (-titan.forward * 10f), ForceMode.Impulse);
        obj2.GetComponent<Rigidbody>().AddTorque(new Vector3((float) UnityEngine.Random.Range(-100, 100), (float) UnityEngine.Random.Range(-100, 100), (float) UnityEngine.Random.Range(-100, 100)), ForceMode.Impulse);
    }

    private void OnTriggerStay(Collider other)
    {
        if (this.active_me)
        {
            if (!this.currentHitsII.Contains(other.gameObject))
            {
                this.currentHitsII.Add(other.gameObject);
                this.currentCamera.GetComponent<IN_GAME_MAIN_CAMERA>().startShake(0.1f, 0.1f, 0.95f);
                if (other.gameObject.transform.root.gameObject.tag == "titan")
                {
                    GameObject obj2;
                    this.currentCamera.GetComponent<IN_GAME_MAIN_CAMERA>().main_object.GetComponent<Hero>().slashHit.Play();
                    obj2 = PhotonNetwork.Instantiate("hitMeat", base.transform.position, Quaternion.Euler(270f, 0f, 0f), 0);
                    obj2.transform.position = base.transform.position;
                    Equipment.Weapon.Use(0);
                }
            }
            if (other.gameObject.tag == "playerHitbox")
            {
                if (FengGameManagerMKII.Gamemode.Settings.Pvp != PvpMode.Disabled)
                {
                    float b = 1f - (Vector3.Distance(other.gameObject.transform.position, base.transform.position) * 0.05f);
                    b = Mathf.Min(1f, b);
                    HitBox component = other.gameObject.GetComponent<HitBox>();
                    if ((((component != null) && (component.transform.root != null)) && (component.transform.root.GetComponent<Hero>().myTeam != this.myTeam)) && !component.transform.root.GetComponent<Hero>().isInvincible())
                    {
                        if (PhotonNetwork.offlineMode)
                        {
                            if (!component.transform.root.GetComponent<Hero>().isGrabbed)
                            {
                                Vector3 vector = component.transform.root.transform.position - base.transform.position;
                                component.transform.root.GetComponent<Hero>().die((Vector3) (((vector.normalized * b) * 1000f) + (Vector3.up * 50f)), false);
                            }
                        }
                        else if ((!component.transform.root.GetComponent<Hero>().HasDied()) && !component.transform.root.GetComponent<Hero>().isGrabbed)
                        {
                            component.transform.root.GetComponent<Hero>().markDie();
                            object[] parameters = new object[5];
                            Vector3 vector2 = component.transform.root.position - base.transform.position;
                            parameters[0] = (Vector3) (((vector2.normalized * b) * 1000f) + (Vector3.up * 50f));
                            parameters[1] = false;
                            parameters[2] = base.transform.root.gameObject.GetPhotonView().viewID;
                            parameters[3] = PhotonView.Find(base.transform.root.gameObject.GetPhotonView().viewID).owner.CustomProperties[PhotonPlayerProperty.name];
                            parameters[4] = false;
                            component.transform.root.GetComponent<Hero>().photonView.RPC("netDie", PhotonTargets.All, parameters);
                        }
                    }
                }
            }
            else if (other.gameObject.tag == "titanneck")
            {
                HitBox item = other.gameObject.GetComponent<HitBox>();
                if (((item != null) && this.checkIfBehind(item.transform.root.gameObject)) && !this.currentHits.Contains(item))
                {
                    item.hitPosition = (Vector3) ((base.transform.position + item.transform.position) * 0.5f);
                    this.currentHits.Add(item);
                    this.meatDie.Play();
                    if (item.transform.root.GetComponent<MindlessTitan>() != null)
                    {
                        Vector3 vector4 = this.currentCamera.GetComponent<IN_GAME_MAIN_CAMERA>().main_object.GetComponent<Rigidbody>().velocity - item.transform.root.GetComponent<Rigidbody>().velocity;
                        var damage = (int)((vector4.magnitude * 10f) * this.scoreMulti);
                        damage = Mathf.Max(10, damage);
                        var mindlessTitan = item.transform.root.GetComponent<MindlessTitan>();
                        if (PhotonNetwork.isMasterClient)
                        {
                            mindlessTitan.OnNapeHitRpc(transform.root.gameObject.GetPhotonView().viewID, damage);
                        }
                        else
                        {
                            mindlessTitan.photonView.RPC("OnNapeHitRpc", mindlessTitan.photonView.owner, transform.root.gameObject.GetPhotonView().viewID, damage);
                        }
                    }
                    else if (!PhotonNetwork.isMasterClient)
                    {
                        if (item.transform.root.GetComponent<FEMALE_TITAN>() != null)
                        {
                            Equipment.Weapon.Use(0x7fffffff);
                            Vector3 vector5 = this.currentCamera.GetComponent<IN_GAME_MAIN_CAMERA>().main_object.GetComponent<Rigidbody>().velocity - item.transform.root.GetComponent<Rigidbody>().velocity;
                            int num4 = (int) ((vector5.magnitude * 10f) * this.scoreMulti);
                            num4 = Mathf.Max(10, num4);
                            if (!item.transform.root.GetComponent<FEMALE_TITAN>().hasDie)
                            {
                                object[] objArray3 = new object[] { base.transform.root.gameObject.GetPhotonView().viewID, num4 };
                                item.transform.root.GetComponent<FEMALE_TITAN>().photonView.RPC("titanGetHit", item.transform.root.GetComponent<FEMALE_TITAN>().photonView.owner, objArray3);
                            }
                        }
                        else if (item.transform.root.GetComponent<COLOSSAL_TITAN>() != null)
                        {
                            Equipment.Weapon.Use(0x7fffffff);
                            if (!item.transform.root.GetComponent<COLOSSAL_TITAN>().hasDie)
                            {
                                Vector3 vector6 = this.currentCamera.GetComponent<IN_GAME_MAIN_CAMERA>().main_object.GetComponent<Rigidbody>().velocity - item.transform.root.GetComponent<Rigidbody>().velocity;
                                int num5 = (int) ((vector6.magnitude * 10f) * this.scoreMulti);
                                num5 = Mathf.Max(10, num5);
                                object[] objArray4 = new object[] { base.transform.root.gameObject.GetPhotonView().viewID, num5 };
                                item.transform.root.GetComponent<COLOSSAL_TITAN>().photonView.RPC("titanGetHit", item.transform.root.GetComponent<COLOSSAL_TITAN>().photonView.owner, objArray4);
                            }
                        }
                        else if (item.transform.root.GetComponent<DummyTitan>())
                        {
                            DummyNapeHit(item.transform.root.GetComponent<DummyTitan>());
                        }
                    }
                    else if (item.transform.root.GetComponent<FEMALE_TITAN>() != null)
                    {
                        Equipment.Weapon.Use(0x7fffffff);
                        if (!item.transform.root.GetComponent<FEMALE_TITAN>().hasDie)
                        {
                            Vector3 vector8 = this.currentCamera.GetComponent<IN_GAME_MAIN_CAMERA>().main_object.GetComponent<Rigidbody>().velocity - item.transform.root.GetComponent<Rigidbody>().velocity;
                            int num7 = (int) ((vector8.magnitude * 10f) * this.scoreMulti);
                            num7 = Mathf.Max(10, num7);
                            if (PlayerPrefs.HasKey("EnableSS") && (PlayerPrefs.GetInt("EnableSS") == 1))
                            {
                                GameObject.Find("MainCamera").GetComponent<IN_GAME_MAIN_CAMERA>().startSnapShot2(item.transform.position, num7, null, 0.02f);
                            }
                            item.transform.root.GetComponent<FEMALE_TITAN>().titanGetHit(base.transform.root.gameObject.GetPhotonView().viewID, num7);
                        }
                    }
                    else if (item.transform.root.GetComponent<COLOSSAL_TITAN>() != null)
                    {
                        Equipment.Weapon.Use(0x7fffffff);
                        if (!item.transform.root.GetComponent<COLOSSAL_TITAN>().hasDie)
                        {
                            Vector3 vector9 = this.currentCamera.GetComponent<IN_GAME_MAIN_CAMERA>().main_object.GetComponent<Rigidbody>().velocity - item.transform.root.GetComponent<Rigidbody>().velocity;
                            int num8 = (int) ((vector9.magnitude * 10f) * this.scoreMulti);
                            num8 = Mathf.Max(10, num8);
                            if (PlayerPrefs.HasKey("EnableSS") && (PlayerPrefs.GetInt("EnableSS") == 1))
                            {
                                GameObject.Find("MainCamera").GetComponent<IN_GAME_MAIN_CAMERA>().startSnapShot2(item.transform.position, num8, null, 0.02f);
                            }
                            item.transform.root.GetComponent<COLOSSAL_TITAN>().titanGetHit(base.transform.root.gameObject.GetPhotonView().viewID, num8);
                        }
                    }
                    else if (item.transform.root.GetComponent<DummyTitan>())
                    {
                        DummyNapeHit(item.transform.root.GetComponent<DummyTitan>());
                    }
                    this.showCriticalHitFX();
                }
            }
            else if (other.gameObject.tag == "titaneye")
            {
                if (!this.currentHits.Contains(other.gameObject))
                {
                    this.currentHits.Add(other.gameObject);
                    GameObject gameObject = other.gameObject.transform.root.gameObject;
                    if (gameObject.GetComponent<FEMALE_TITAN>() != null)
                    {
                        if (!PhotonNetwork.isMasterClient)
                        {
                            if (!gameObject.GetComponent<FEMALE_TITAN>().hasDie)
                            {
                                object[] objArray5 = new object[] { base.transform.root.gameObject.GetPhotonView().viewID };
                                gameObject.GetComponent<FEMALE_TITAN>().photonView.RPC("hitEyeRPC", PhotonTargets.MasterClient, objArray5);
                            }
                        }
                        else if (!gameObject.GetComponent<FEMALE_TITAN>().hasDie)
                        {
                            gameObject.GetComponent<FEMALE_TITAN>().hitEyeRPC(base.transform.root.gameObject.GetPhotonView().viewID);
                        }
                    }
                    else if (gameObject.GetComponent<MindlessTitan>() != null)
                    {
                        Vector3 vector4 = this.currentCamera.GetComponent<IN_GAME_MAIN_CAMERA>().main_object.GetComponent<Rigidbody>().velocity - gameObject.GetComponent<Rigidbody>().velocity;
                        var damage = (int)((vector4.magnitude * 10f) * this.scoreMulti);
                        damage = Mathf.Max(10, damage);
                        var mindlessTitan = gameObject.GetComponent<MindlessTitan>();
                        if (PhotonNetwork.isMasterClient)
                        {
                            mindlessTitan.OnEyeHitRpc(transform.root.gameObject.GetPhotonView().viewID, damage);
                        }
                        else
                        {
                            mindlessTitan.photonView.RPC("OnEyeHitRpc", mindlessTitan.photonView.owner, transform.root.gameObject.GetPhotonView().viewID, damage);
                        }
                        showCriticalHitFX();
                    }
                }
            }
            else if (other.gameObject.tag == "titanbodypart")
            {
                if (!this.currentHits.Contains(other.gameObject))
                {
                    this.currentHits.Add(other.gameObject);
                    GameObject gameObject = other.gameObject.transform.root.gameObject;
                    if (gameObject.GetComponent<MindlessTitan>() != null)
                    {
                        Vector3 vector4 = this.currentCamera.GetComponent<IN_GAME_MAIN_CAMERA>().main_object.GetComponent<Rigidbody>().velocity - gameObject.GetComponent<Rigidbody>().velocity;
                        var damage = (int)((vector4.magnitude * 10f) * this.scoreMulti);
                        damage = Mathf.Max(10, damage);
                        var mindlessTitan = gameObject.GetComponent<MindlessTitan>();
                        var body = mindlessTitan.TitanBody.GetBodyPart(other.transform);
                        if (PhotonNetwork.isMasterClient)
                        {
                            mindlessTitan.OnBodyPartHitRpc(body, damage);
                        }
                        else
                        {
                            mindlessTitan.photonView.RPC("OnBodyPartHitRpc", mindlessTitan.photonView.owner, body, damage);
                        }
                    }
                }
            }
            else if ((other.gameObject.tag == "titanankle") && !this.currentHits.Contains(other.gameObject))
            {
                this.currentHits.Add(other.gameObject);
                GameObject obj4 = other.gameObject.transform.root.gameObject;
                Vector3 vector10 = Vector3.zero;
                if (obj4.GetComponent<Rigidbody>())//patch for dummy titan
                {
                    vector10 = this.currentCamera.GetComponent<IN_GAME_MAIN_CAMERA>().main_object.GetComponent<Rigidbody>().velocity - obj4.GetComponent<Rigidbody>().velocity;
                }
                int num9 = (int) ((vector10.magnitude * 10f) * this.scoreMulti);
                num9 = Mathf.Max(10, num9);
                if (obj4.GetComponent<MindlessTitan>() != null)
                {
                    var mindlessTitan = obj4.GetComponent<MindlessTitan>();
                    mindlessTitan.OnAnkleHit(transform.root.gameObject.GetPhotonView().viewID, num9);
                    this.showCriticalHitFX();
                }
                else if (obj4.GetComponent<FEMALE_TITAN>() != null)
                {
                    if (other.gameObject.name == "ankleR")
                    {
                        if (!PhotonNetwork.isMasterClient)
                        {
                            if (!obj4.GetComponent<FEMALE_TITAN>().hasDie)
                            {
                                object[] objArray8 = new object[] { base.transform.root.gameObject.GetPhotonView().viewID, num9 };
                                obj4.GetComponent<FEMALE_TITAN>().photonView.RPC("hitAnkleRRPC", PhotonTargets.MasterClient, objArray8);
                            }
                        }
                        else if (!obj4.GetComponent<FEMALE_TITAN>().hasDie)
                        {
                            obj4.GetComponent<FEMALE_TITAN>().hitAnkleRRPC(base.transform.root.gameObject.GetPhotonView().viewID, num9);
                        }
                    }
                    else if (!PhotonNetwork.isMasterClient)
                    {
                        if (!obj4.GetComponent<FEMALE_TITAN>().hasDie)
                        {
                            object[] objArray9 = new object[] { base.transform.root.gameObject.GetPhotonView().viewID, num9 };
                            obj4.GetComponent<FEMALE_TITAN>().photonView.RPC("hitAnkleLRPC", PhotonTargets.MasterClient, objArray9);
                        }
                    }
                    else if (!obj4.GetComponent<FEMALE_TITAN>().hasDie)
                    {
                        obj4.GetComponent<FEMALE_TITAN>().hitAnkleLRPC(base.transform.root.gameObject.GetPhotonView().viewID, num9);
                    }
                    this.showCriticalHitFX();
                }
                else if(obj4.GetComponent<DummyTitan>())
                {
                    this.showCriticalHitFX();
                }
            }
        }
    }

    private void showCriticalHitFX()
    {
        GameObject obj2;
        this.currentCamera.GetComponent<IN_GAME_MAIN_CAMERA>().startShake(0.2f, 0.3f, 0.95f);
        obj2 = PhotonNetwork.Instantiate("redCross", base.transform.position, Quaternion.Euler(270f, 0f, 0f), 0);
        obj2.transform.position = base.transform.position;
    }

    private void Start()
    {
        this.currentCamera = GameObject.Find("MainCamera");
        Equipment = base.transform.root.GetComponent<Equipment>();
    }
}


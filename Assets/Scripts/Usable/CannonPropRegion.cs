using Photon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

public class CannonPropRegion : Photon.MonoBehaviour
{
    public bool destroyed;
    public bool disabled;
    public string settings;
    public Hero storedHero;

    [SerializeField]
    private Cannon.Type type = Cannon.Type.Ground;

    [SerializeField]
    private bool autoGenerateSettings = true;

    private static readonly Dictionary<Cannon.Type, string> PrefabNameByType = new Dictionary<Cannon.Type, string>
    {
        { Cannon.Type.Ground, "CannonGround" },
        { Cannon.Type.Wall, "CannonWall" }
    };

    public void OnDestroy()
    {
        if (this.storedHero != null)
        {
            this.storedHero.myCannonRegion = null;
            this.storedHero.ClearPopup();
        }
    }

    public void OnTriggerEnter(Collider collider)
    {
        GameObject gameObject = collider.transform.root.gameObject;
        if ((gameObject.layer == 8) && gameObject.GetPhotonView().isMine)
        {
            Hero component = gameObject.GetComponent<Hero>();
            if ((component != null) && !component.isCannon)
            {
                if (component.myCannonRegion != null)
                {
                    component.myCannonRegion.storedHero = null;
                }
                component.myCannonRegion = this;
                this.storedHero = component;
            }
        }
    }

    public void OnTriggerExit(Collider collider)
    {
        GameObject gameObject = collider.transform.root.gameObject;
        if ((gameObject.layer == 8) && gameObject.GetPhotonView().isMine)
        {
            Hero component = gameObject.GetComponent<Hero>();
            if (((component != null) && (this.storedHero != null)) && (component == this.storedHero))
            {
                component.myCannonRegion = null;
                component.ClearPopup();
                this.storedHero = null;
            }
        }
    }

    [PunRPC]
    public void RequestControlRPC(int viewID, PhotonMessageInfo info)
    {
        if (!((!base.photonView.isMine || !PhotonNetwork.isMasterClient) || this.disabled))
        {
            Hero component = PhotonView.Find(viewID).gameObject.GetComponent<Hero>();
            if (((component != null) && (component.photonView.owner == info.sender)) && !FengGameManagerMKII.instance.allowedToCannon.ContainsKey(info.sender.ID))
            {
                this.disabled = true;
                base.StartCoroutine(this.WaitAndEnable());
                FengGameManagerMKII.instance.allowedToCannon.Add(info.sender.ID, new CannonValues(base.photonView.viewID, this.settings));
                component.photonView.RPC("SpawnCannonRPC", info.sender, new object[] { this.settings });
            }
        }
    }

    [PunRPC]
    public void SetSize(string settings, PhotonMessageInfo info)
    {
        if (info.sender.isMasterClient)
        {
            string[] strArray = settings.Split(new char[] { ',' });
            if (strArray.Length > 15)
            {
                float a = 1f;
                GameObject gameObject = null;
                gameObject = base.gameObject;
                if (strArray[2] != "default")
                {
                    if (strArray[2].StartsWith("transparent"))
                    {
                        float num2;
                        if (float.TryParse(strArray[2].Substring(11), out num2))
                        {
                            a = num2;
                        }
                        foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>())
                        {
                            renderer.material = (Material) FengGameManagerMKII.RCassets.LoadAsset("transparent");
                            if ((Convert.ToSingle(strArray[10]) != 1f) || (Convert.ToSingle(strArray[11]) != 1f))
                            {
                                renderer.material.mainTextureScale = new Vector2(renderer.material.mainTextureScale.x * Convert.ToSingle(strArray[10]), renderer.material.mainTextureScale.y * Convert.ToSingle(strArray[11]));
                            }
                        }
                    }
                    else
                    {
                        foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>())
                        {
                            renderer.material = (Material) FengGameManagerMKII.RCassets.LoadAsset(strArray[2]);
                            if ((Convert.ToSingle(strArray[10]) != 1f) || (Convert.ToSingle(strArray[11]) != 1f))
                            {
                                renderer.material.mainTextureScale = new Vector2(renderer.material.mainTextureScale.x * Convert.ToSingle(strArray[10]), renderer.material.mainTextureScale.y * Convert.ToSingle(strArray[11]));
                            }
                        }
                    }
                }
                float x = gameObject.transform.localScale.x * Convert.ToSingle(strArray[3]);
                x -= 0.001f;
                float y = gameObject.transform.localScale.y * Convert.ToSingle(strArray[4]);
                float z = gameObject.transform.localScale.z * Convert.ToSingle(strArray[5]);
                gameObject.transform.localScale = new Vector3(x, y, z);
                if (strArray[6] != "0")
                {
                    Color color = new Color(Convert.ToSingle(strArray[7]), Convert.ToSingle(strArray[8]), Convert.ToSingle(strArray[9]), a);
                    foreach (MeshFilter filter in gameObject.GetComponentsInChildren<MeshFilter>())
                    {
                        Mesh mesh = filter.mesh;
                        Color[] colorArray = new Color[mesh.vertexCount];
                        for (int i = 0; i < mesh.vertexCount; i++)
                        {
                            colorArray[i] = color;
                        }
                        mesh.colors = colorArray;
                    }
                }
            }
        }
    }

    public void Start()
    {
        if (((int) FengGameManagerMKII.settings[0x40]) >= 100)
        {
            base.GetComponent<Collider>().enabled = false;
        }
    }

    public IEnumerator WaitAndEnable()
    {
        yield return new WaitForSeconds(5f);
        if (!this.destroyed)
        {
            this.disabled = false;
        }
    }

    private void OnValidate()
    {
        if (this.autoGenerateSettings)
        {
            var pos = this.transform.position;
            var rot = this.transform.rotation;
            var prefabName = PrefabNameByType[this.type];
            this.settings = $"photon,{prefabName},{pos.x},{pos.y},{pos.z},{rot.x},{rot.y},{rot.z},{rot.w}";
        }
    }
}
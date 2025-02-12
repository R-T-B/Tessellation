using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ParallaxShader;
using ModuleWheels;
using System.CodeDom;
using System.Security.Cryptography;

namespace ParallaxCollision
{
    public class ParallaxWheelPhysicsComponent : PartModule
    {
        //Modders should implement this on their own wheels / landing gear if they want collisions to work
        //Add this component to the wheel with the correct transform.position of the wheel hub
        //This allows Parallax to know where the physical wheel is so it can position the plane under the wheel correctly.
        //Works for landing gear too, but modders will need to instead specify the landing leg foot
        public static Vector3 wheelCenter = Vector3.zero;
        public static Transform wheelTransform;

    }
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class PhysicsStarter : MonoBehaviour
    {
        public bool started = false;
        public static int index = 0;
        public Ray vesselRay;
        public RaycastHit vesselHit;
        public int layerMask = (int)(1 << 29);
        public static Vector3 terrainNormal = new Vector3(0, 1, 0);
        public void Start()
        {
            Camera.current.cullingMask = Camera.current.cullingMask | (1 << 29);    //See the subdivided terrain - Make layer 29 visible (unused by ksp)
            Camera.main.cullingMask = Camera.main.cullingMask | (1 << 29);
            Sun.Instance.sunLight.cullingMask = Sun.Instance.sunLight.cullingMask | (1 << 29);
            
        }
        public void Update()
        {
            if (ParallaxSettings.collide == false)
            {
                return;
            }
            bool key = Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha7);
            if (key)
            {
                //index++;
                foreach (Part p in FlightGlobals.ActiveVessel.parts)
                {
                    if (p.GetComponent<ParallaxPhysics>() != null)
                    {
                        Debug.Log("Part has parallax physics - restarting!");
                        p.GetComponent<ParallaxPhysics>().RestartWheels(p.GetComponent<ParallaxPhysics>().p);
                    }
                }
            }
        }
        public void FixedUpdate()
        {
            Vector3 planetNormal = -Vector3.Normalize(FlightGlobals.ActiveVessel.transform.position - FlightGlobals.currentMainBody.transform.position);
            vesselRay.origin = FlightGlobals.ActiveVessel.transform.position;
            vesselRay.direction = planetNormal;
            if (UnityEngine.Physics.Raycast(vesselRay, out vesselHit, 10.1f, layerMask))
            {
                terrainNormal = vesselHit.normal;   //remember to flip this negative since raycast needs to go backwards down the normal!!
            }
            else
            {
                terrainNormal = planetNormal;
            }
        }
        public static Vector3 GetPos(List<Transform> trans)
        {
            Vector3 pos = trans[index].position;
            if (pos == Vector3.zero)
            {
                return trans[index].localPosition;
            }
            return pos;
        }
        public static Vector3 GetPos(Transform trans)
        {
            Vector3 pos = trans.position;
            if (pos == Vector3.zero)
            {
                return trans.localPosition;
            }
            return pos;
        }
        //public void Update()
        //{
        //    bool key = Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha7);
        //    if (key)
        //    {
        //        Debug.Log(FlightGlobals.ActiveVessel.name);
        //        FlightGlobals.ActiveVessel.parts[0].gameObject.AddComponent<ParallaxPhysics>();
        //        
        //    }
        //    bool key2 = Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha6);
        //    if (key2)
        //    {
        //        
        //        if (PhysicsValidator.planes.Count() > 0)
        //        {
        //            PhysicsValidator.planes.Clear();
        //        }
        //        
        //        Debug.Log(FlightGlobals.ActiveVessel.name);
        //        foreach (Part p in FlightGlobals.ActiveVessel.parts)
        //        {
        //            if (p.Modules.Contains<ModuleWheelBase>())
        //            {
        //                //Vector3 centre = p.GetComponent<ModuleWheelBase>().center;
        //                p.gameObject.AddComponent<ParallaxPhysics>();
        //                foreach (Component c in p.gameObject.GetComponentsInChildren(typeof(Component)))
        //                {
        //                    Debug.Log(c.name + ", at " + c.transform.position.ToString("F3"));
        //                    Debug.Log("With local position " + c.transform.localPosition.ToString("F3"));
        //                }
        //            }
        //        }
        //        
        //        started = true;
        //
        //    }
        //}
    }
    public class PhysicsValidator : MonoBehaviour
    {
        public static Dictionary<GameObject, bool> planes = new Dictionary<GameObject, bool>();
        public static int planeCount = 0;
        public void Update()
        {
            //foreach (KeyValuePair<GameObject, bool> plane in planes)
            //{
            //    if (plane.Value == false)
            //    {
            //        return;
            //    }
            //    
            //}
            //foreach (KeyValuePair<GameObject, bool> plane in planes)
            //{
            //    plane.Key.layer = 15;
            //}
        }
    }
    public class ParallaxPhysicsManager : VesselModule
    {

        public static List<Transform> transforms = new List<Transform>();
        
        public override void OnLoadVessel()
        {
            if (ParallaxSettings.collide == false)
            {
                return;
            }
            foreach (Part p in vessel.parts)
            {
                if (vessel.parts.Count == 1)
                {
                    if (vessel.parts[0].isKerbalEVA() == true)
                    {
                        Debug.Log("This vessel is a Kerbal");
                        p.gameObject.AddComponent<ParallaxPhysics>();
                        var physicsComponent = p.gameObject.GetComponent<ParallaxPhysics>();
                        physicsComponent.transform.parent = p.gameObject.transform;
                        physicsComponent.p = p;
                        physicsComponent.origin = p.gameObject.transform.position;
                        physicsComponent.wheelPivot = physicsComponent;
                    }
                }
                if ((p.Modules.Contains<ModuleWheelBase>() || p.Modules.Contains<ParallaxWheelPhysicsComponent>()
                  || p.Modules.Contains("KSPWheelBase") || p.Modules.Contains<ModuleGroundSciencePart>() || p.Modules.Contains<ModuleGroundPart>()
                  || p.Modules.Contains<ModuleGroundExperiment>() || p.Modules.Contains<ModuleGroundCommsPart>() || p.Modules.Contains<ModulePhysicMaterial>()
                  ) && p.gameObject.GetComponent<ParallaxPhysics>() == null)
                {
                    if (p.Modules.Contains("KSPWheelBase"))
                    {
                        Debug.Log("Modded wheel detected - KSPWheelBase component");
                    }
                    p.gameObject.AddComponent<ParallaxPhysics>();
                    
                    var physicsComponent = p.gameObject.GetComponent<ParallaxPhysics>();
                    physicsComponent.p = p;
                    foreach (Component c in p.gameObject.GetComponentsInChildren(typeof(Component)))
                    {
                        if (c.GetType().Name is "KSPWheelBase")
                        {
                            physicsComponent.origin = PhysicsStarter.GetPos(c.transform);
                            physicsComponent.wheelPivot = c;
                        }
                        if (c.name == "WheelCollider")
                        {
                            physicsComponent.origin = PhysicsStarter.GetPos(c.transform);
                            physicsComponent.wheelPivot = c;
                        }
                        else if (c.name == "foot")
                        {
                            physicsComponent.origin = PhysicsStarter.GetPos(c.transform);
                            physicsComponent.wheelPivot = c;
                        }
                        else if (c.name == "leg_collider")
                        {
                            physicsComponent.origin = PhysicsStarter.GetPos(c.transform);
                            physicsComponent.wheelPivot = c;
                        }
                        else if (c is ModuleGroundSciencePart || c is ModuleGroundSciencePart || c is ModuleGroundPart
                  || c is ModuleGroundExperiment || c is ModuleGroundCommsPart || c is ModulePhysicMaterial)
                        {
                            physicsComponent.origin = PhysicsStarter.GetPos(c.transform);
                            physicsComponent.wheelPivot = c;
                        }
                        else
                        {
                            //Can't find the component
                        }
                    }
                }
                if (p.gameObject.name == "miniLandingLeg")
                {
                    foreach (Transform d in p.gameObject.GetComponentsInChildren(typeof(Transform)))
                    {
                        transforms.Add(d);
                    }
                    foreach (Transform d in p.gameObject.GetComponents(typeof(Transform)))
                    {
                        transforms.Add(d);
                    }
                }
                if (p.Modules.Contains<ModuleWheelDeployment>())
                {
                    p.gameObject.GetComponent<ParallaxPhysics>().wheelDeploy = p.Modules.GetModule<ModuleWheelDeployment>();
                    if (p.Modules.GetModule<ModuleWheelDeployment>().position < 1)
                    {
                        p.gameObject.GetComponent<ParallaxPhysics>().wheelsHaveBeenRetracted = true;
                        Debug.Log("A wheel is retracted, not starting collisions");
                    }
                    //Debug.Log("ModuleWheelDeployment exists here");
                    //ModuleWheelDeployment mwd = p.Modules.GetModule<ModuleWheelDeployment>();
                    //mwd.on_deploy.OnEvent += DisableComponentOnGearRaise;

                    //Do shit here that actually works
                }
            }
        }


        public override void OnUnloadVessel()
        {
            if (ParallaxSettings.collide == false)
            {
                return;
            }
            foreach (Part p in vessel.parts)
            {
                if (p.Modules.Contains<ModuleWheelBase>() || p.Modules.Contains<ParallaxWheelPhysicsComponent>())
                {
                    if (p.gameObject.GetComponent<ParallaxPhysics>() != null)
                    {
                        Destroy(gameObject.GetComponent<ParallaxPhysics>());
                        Debug.Log("Destroyed ParallaxPhysics as vessel has been unloaded");
                    }

                }
            }
        }
    }
    public class ParallaxPhysics : MonoBehaviour
    {
        // Start is called before the first frame update
        List<Transform> transforms = new List<Transform>();
        GameObject plane;
        public LayerMask ignore;
        private Ray approximateRay;
        private RaycastHit hitApproximateRay;
        Vector3 currVel = Vector3.zero;
        Vector3 samplePoint;
        Vector3 nextPoint;
        Vector3 sampleNormal;
        Vector2 _ST;
        Texture2D tex;
        Texture2D normalLow;
        Texture2D normalMid;
        Texture2D normalHigh;
        Texture2D normalSteep;
        float lastDisplacement = 10000;
        Vector3 camDisplacement;
        float _Displacement_Scale = 0;
        float blendLowStart;
        float blendLowEnd;
        float blendHighStart;
        float blendHighEnd;
        float steepPower;
        Vector3 planetOrigin;
        float planetRadius;
        string thisBody = "";
        int layerMask = (int)(1 << 29); //Raycast on layer 29 to hit the collider belonging to the subdivided quad
        float _Displacement_Offset = 0;
        bool started = false;
        int dampingFrames = 0;
        public Vector3 origin = Vector3.zero;
        public Component wheelPivot;
        float bounds = 1;
        bool packed = false;
        public Component wheelDeploy;
        public bool wheelsHaveBeenRetracted = false;
        public bool wheelsHaveBeenInWarp = false;
        bool craftOvertipped = false;
        public Part p;
        //public Collider DetermineCollider()
        //{
        //    if (p != null)
        //    {
        //        return p.collider;
        //    }
        //    else
        //    {
        //        Debug.Log("Part is null");
        //        return null;
        //        
        //    }
        //    //if (p.gameObject.GetComponent<Collider>() != null)
        //    //{
        //    //    return p.gameObject.GetComponent<Collider>();
        //    //}
        //    //if (p.gameObject.GetComponent<MeshCollider>() != null)
        //    //{
        //    //    return p.gameObject.GetComponent<MeshCollider>();
        //    //}
        //    //if (p.gameObject.GetComponent<SphereCollider>() != null)
        //    //{
        //    //    return p.gameObject.GetComponent<SphereCollider>();
        //    //}
        //    //return null;
        //}
        //public void OnCollisionEnter(Collision collision)   //This object: WHEEL. Colliding object: PLANE
        //{
        //    Debug.Log("Collision!");
        //    if (collision.gameObject.name != plane.name && collision.gameObject.name.StartsWith("ParallaxPlane-"))
        //    {
        //        Debug.Log("Ignoring");
        //        Collider c = DetermineCollider();
        //        if (c != null)
        //        {
        //            Physics.IgnoreCollision(collision.collider, c);
        //        }
        //        else
        //        {
        //            Debug.Log("c is null");
        //        }
        //        
        //        
        //    }
        //    Debug.Log("Finished colliding - ignored");
        //}
        void Start()
        {
            if (FlightGlobals.currentMainBody == FlightGlobals.GetBodyByName("Minmus") && ParallaxSettings.flatMinmus == true)
            {
                return;
            }
            Debug.Log("Starting physics!");
            //float dist = gameObject.GetColliderBounds().size.y; //We need to make the plane start below the wheel
            plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plane.layer = 15;                                        //Set initial layer such that raycast ignores the plane
            plane.SetActive(true);
            plane.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            plane.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            plane.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            plane.transform.localPosition = Vector3.zero;
            plane.transform.position = new Vector3(1000, 1000, 1000);// gameObject.transform.position;
            plane.GetComponent<MeshRenderer>().enabled = false;
            plane.GetComponent<Collider>().isTrigger = false;
            plane.GetComponent<Collider>().enabled = false;
            plane.tag = FlightGlobals.currentMainBody.BiomeMap.GetAtt(FlightGlobals.ActiveVessel.latitude * UtilMath.Deg2Rad, FlightGlobals.ActiveVessel.longitude * UtilMath.Deg2Rad).name;
            plane.name = "ParallaxPlane-" + PhysicsValidator.planeCount.ToString();
            PhysicsValidator.planeCount++;
            //Vector3 meshbounds = gameObject.GetComponent<MeshFilter>().mesh.bounds.size;
            //bounds = Mathf.Max(meshbounds.x, meshbounds.y, meshbounds.z);
            //plane.transform.localScale = new Vector3(bounds / 4, 0.01f, bounds / 4);
            PhysicsValidator.planes.Add(plane, false);




        }
        public void RestartWheels(Part p) //Add the component back to wheels once the component has been deleted
        {
            foreach (Component c in p.gameObject.GetComponentsInChildren(typeof(Component)))
            {
                if (c.name == "wheel")
                {
                    origin = PhysicsStarter.GetPos(c.transform);
                    wheelPivot = c;
                }
                if (c.name == "WheelCollider")
                {
                    origin = PhysicsStarter.GetPos(c.transform);
                    wheelPivot = c;
                }
                else if (c.name == "foot")
                {
                    origin = PhysicsStarter.GetPos(c.transform);
                    wheelPivot = c;
                }
                else if (c.name == "leg_collider")
                {
                    origin = PhysicsStarter.GetPos(c.transform);
                    wheelPivot = c;
                }
                else if ( c.GetType().Name == "KSPWheelBase")
                {
                    origin = PhysicsStarter.GetPos(c.transform);
                    wheelPivot = c;
                }
                else if (c is ModuleGroundSciencePart || c is ModuleGroundSciencePart || c is ModuleGroundPart
                  || c is ModuleGroundExperiment || c is ModuleGroundCommsPart || c is ModulePhysicMaterial)
                {
                    origin = PhysicsStarter.GetPos(c.transform);
                    wheelPivot = c;
                }
                else
                {
                }
            }
            Debug.Log("[Parallax Collisions] Restarted wheels");
        }
        public void RestartWheelsAfterTimeWarp(Part p)
        {

        }
        //public int CheckIfWheelsAreUnderTerrain(Vector3 falseGroundPos, Vector3 samplePos, float rayDistance)
        //{
        //    int dampingFrames = 150;
        //    if (Vector3.Distance(falseGroundPos, samplePos) > rayDistance)
        //    {
        //        //Wheels are inside the terrain, set damping frames to 0
        //        dampingFrames = 0;
        //    }
        //
        //    return dampingFrames;
        //}
        // Physics update is 50 times per second
        public void DisablePlane()
        {
            if (plane != null)
            {
                plane.GetComponent<Collider>().enabled = false;
            }
        }
        public void EnablePlane()
        {
            if (plane != null)
            {
                plane.GetComponent<Collider>().enabled = true;
            }
        }
        void Update()
        {
            
            if (FlightGlobals.currentMainBody == FlightGlobals.GetBodyByName("Minmus") && ParallaxSettings.flatMinmus == true)
            {
                DisablePlane();
                return;
            }
            CheckIfEnabled();
            if (wheelDeploy != null)
            {
                if ((wheelDeploy as ModuleWheelDeployment).deployedPosition == 1)
                {
                    if ((wheelDeploy as ModuleWheelDeployment).Position < 1)
                    {
                        wheelsHaveBeenRetracted = true;
                        DisablePlane();
                        return;
                    }
                }
                else if ((wheelDeploy as ModuleWheelDeployment).deployedPosition == 0)
                {
                    if ((wheelDeploy as ModuleWheelDeployment).Position > 0)
                    {
                        wheelsHaveBeenRetracted = true;
                        DisablePlane();
                        return;
                    }
                }
                if (wheelsHaveBeenRetracted == true)
                {
                    RestartWheels(p);
                    Debug.Log("Restarting after retract");
                    dampingFrames = 0;
                    wheelsHaveBeenRetracted = false;
                }
            }
            
            if (wheelsHaveBeenRetracted == true)
            {
                DisablePlane();
                return;
            }
            
            if (TimeWarp.CurrentRate > 1 && TimeWarp.WarpMode == TimeWarp.Modes.HIGH)
            {
                wheelsHaveBeenInWarp = true;
                DisablePlane();
                return;
            }
            
            if ((wheelsHaveBeenInWarp == true && TimeWarp.CurrentRate == 1))
            {
                RestartWheels(p);
                dampingFrames = dampingFrames - 10;
                wheelsHaveBeenInWarp = false;
                DisablePlane();
                return;
            }
            
            if (ParallaxOnDemandLoader.finishedMainLoad == false)
            {
                DisablePlane();
                return;
            }
            
            if (p.vessel.packed == true)  //Vessel is time warping so we don't need to do anything here
            {
                DisablePlane();
                packed = true;
                started = true;
                return;
            }
            
            if (packed == true) //Vessel has finished time warping and needs re-initializing
            {
                DisablePlane();
                packed = false;
                Debug.Log("[Parallax Physics] Restarting after unpack...");
                Destroy(plane);
                Start();
            }
            
            //plane.layer = 0;    //Layer has to be set to 0 to avoid raycast on layer 15
            if (p == null)
            {
                DisablePlane();
                return;
            }
            
            if (wheelPivot == null)
            {
                DisablePlane();
                return;
            }
            
            if ((thisBody != FlightGlobals.currentMainBody.name) || started == false)    //Body change, reassign textures
            {
                if (ParallaxShaderLoader.parallaxBodies.ContainsKey(FlightGlobals.currentMainBody.name) == false)
                {
                    return;
                }
                Debug.Log("[Parallax Physics] Body changed! Getting new values...");
                Material a = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial;
                tex = PhysicsTexHolder.displacementTex;
                normalLow = PhysicsTexHolder.physicsTexLow;
                normalMid = PhysicsTexHolder.physicsTexMid;
                normalHigh = PhysicsTexHolder.physicsTexHigh;
                normalSteep = PhysicsTexHolder.physicsTexSteep;
                _ST = (a.GetTextureScale("_SurfaceTexture"));
                blendLowStart = a.GetFloat("_LowStart");
                blendLowEnd = a.GetFloat("_LowEnd");
                blendHighStart = a.GetFloat("_HighStart");
                blendHighEnd = a.GetFloat("_HighEnd");
                steepPower = a.GetFloat("_SteepPower");
                planetOrigin = a.GetVector("_PlanetOrigin");
                planetRadius = a.GetFloat("_PlanetRadius");
                _Displacement_Scale = a.GetFloat("_displacement_scale");
                _Displacement_Offset = a.GetFloat("_displacement_offset");
                Debug.Log("[Parallax Physics] Success!");
                thisBody = FlightGlobals.currentMainBody.name;
                RestartWheels(p);
            }
            
            origin = PhysicsStarter.GetPos(wheelPivot.transform);
            
            if (p.vessel.srf_velocity.magnitude < 0.025f && dampingFrames >= 150) //Vessel is "landed"
            {
                plane.layer = 15;
                started = true;
                return;
            }
            else
            {
                plane.layer = 15;
            }
            
            approximateRay.origin = origin;
            approximateRay.direction = -PhysicsStarter.terrainNormal;
            if (UnityEngine.Physics.Raycast(approximateRay, out hitApproximateRay, 5f, layerMask))
            {
                samplePoint = hitApproximateRay.point;
                sampleNormal = hitApproximateRay.normal;
            }
            else
            {
                started = true;
                return;
            }
            float displacement = GetDisplacement(new Vector2(wheelPivot.transform.position.x, wheelPivot.transform.position.y), tex, _ST);
            //float mass = (float)p.vessel.totalMass;
            //float massInfluence = (mass * (float)FlightGlobals.currentMainBody.GeeASL * 9.8f); //Weight in Newtons
            //massInfluence = (1f - Mathf.Clamp((massInfluence - 186f) / (9800), 0, 1)); //~1000 tons and displacement doesn't matter much. Starts influencing after a 20 ton ship and depends on gravity
            //displacement *= massInfluence;

            float craftUpsideDownNess = Vector3.Dot(PhysicsStarter.terrainNormal, p.transform.up);  //Value of 1 - Wheel is properly oriented. Value of -1 - Wheel is upside-fuckin-down mate
            if (craftUpsideDownNess <= 0.4f && !p.isKerbalEVA())
            {
                //We got 1.3 to play about with
                //Will use 0.3 of it to lower the plane to the ground
                //craftUpsideDownNess = Mathf.Clamp(craftUpsideDownNess, 0, 0.3f) / 0.3f;   //0 to 1
                //displacement *= craftUpsideDownNess;    //Lower plane to the ground when the wheel is tilting too much. Probs played too many FPS games smh
                //dampingFrames = (int)((float)dampingFrames * craftUpsideDownNess);
                dampingFrames = 0;
                //craftOvertipped = true;
                displacement = -50;
                //displacement
            }

            if (lastDisplacement == 10000)
            {
                lastDisplacement = displacement;    //First contact with the ground
            }
            
            Vector3 forward = Vector3.Normalize(gameObject.GetComponent<Rigidbody>().velocity);
            Vector3 tangent = Vector3.Cross(forward, sampleNormal); //Vector to rotate plane around
            Vector3 disp = new Vector3((displacement * _Displacement_Scale + _Displacement_Offset - 0.5f) * sampleNormal.x, (displacement * _Displacement_Scale + _Displacement_Offset - 0.5f) * sampleNormal.y, (displacement * _Displacement_Scale + _Displacement_Offset - 0.5f) * sampleNormal.z);
            
            if (dampingFrames <= 150)
            {
                dampingFrames++;
            }
            if (hitApproximateRay.collider.gameObject.name.Split(' ')[0] != FlightGlobals.currentMainBody.name) //If not casting down to terrain but instead hitting runway / scatter
            {
                plane.GetComponent<Collider>().enabled = false;
                plane.transform.position = samplePoint;
                PhysicsValidator.planes[plane] = true;
                started = true;
                return;
            }
            if (started == false || dampingFrames <= 150)
            {
                if (dampingFrames == 1)
                {
                    ScreenMessages.PostScreenMessage("[Parallax Collisions] Damping...", 3f, ScreenMessageStyle.UPPER_LEFT);
                }
                Vector3 disp2 = new Vector3((displacement * _Displacement_Scale + _Displacement_Offset) * sampleNormal.x, (displacement * _Displacement_Scale + _Displacement_Offset) * sampleNormal.y, (displacement * _Displacement_Scale + _Displacement_Offset) * sampleNormal.z);
                plane.transform.position = samplePoint + (disp2 * ((float)dampingFrames / 150f)) + new Vector3(-0.5f * sampleNormal.x, -0.5f * sampleNormal.y, -0.5f * sampleNormal.z);    //Slowly raise plane from terrain ground
                started = true;
                plane.GetComponent<Collider>().enabled = true;
                PhysicsValidator.planes[plane] = true;
                if (dampingFrames == 150)
                {
                    ScreenMessages.PostScreenMessage("[Parallax Collisions] Finished Damping!", 3f, ScreenMessageStyle.UPPER_LEFT);
                }
                return;
            }

            plane.transform.position = samplePoint + disp;

            float rotationPercentage = 1 - ((lastDisplacement * _Displacement_Scale - _Displacement_Offset) / (displacement * _Displacement_Scale - _Displacement_Offset));
            lastDisplacement = displacement;
            plane.transform.Rotate(tangent, rotationPercentage * 180);
            //plane.GetComponent<Collider>().enabled = true;
            EnablePlane();
            PhysicsValidator.planes[plane] = true;
            started = true;
            
        }
        //public void Update()
        //{
        //    CheckIfEnabled();
        //}
        public void CheckIfEnabled()
        {
            bool key = Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha3);
            if (key)
            {
                plane.GetComponent<MeshRenderer>().enabled = !plane.GetComponent<MeshRenderer>().enabled;
               
            }
        }
        float GetDisplacement(Vector2 uv, Texture2D tex, Vector2 ScaleTransform)
        {
            float displacement = SampleBiplanarTextureCPU(tex, ScaleTransform, nextPoint);
            Vector3 surfaceNormal = new Vector3(0, -1, 0);
            float distance = Vector3.Distance(samplePoint, nextPoint);


            return displacement;
        }
        Vector2 Clamp(Vector2 sampleuV)
        {
            return new Vector2(sampleuV.x % 1, sampleuV.y % 1);
        }
        Vector3 GetNormal(Color[] data, float width)
        {
            /// normalized size of one texel. this would be 1/1024.0 if using 1024x1024 bitmap. 
            float texelSize = 1 / width;

            float n = data[1].r;
            float s = data[7].r;
            float e = data[5].r;
            float w = data[3].r;


            Vector3 ew = Vector3.Normalize(new Vector3(2 * texelSize, e - w, 0));
            Vector3 ns = Vector3.Normalize(new Vector3(0, s - n, 2 * texelSize));
            Vector3 result = Vector3.Cross(ew, ns);

            return result;
        }
        public float SampleBiplanarTextureCPU(Texture2D tex, Vector2 _ST, Vector3 nextPoint)
        {

            Vector3 floatUV = Position.floatUV; //SurfaceTextureUVs
            //abs(dot(normalize(o.world_vertex - _PlanetOrigin), normalize(o.normalDir))); (from shader)
            float slope = Mathf.Abs(Vector3.Dot(Vector3.Normalize(samplePoint - planetOrigin), Vector3.Normalize(sampleNormal)));
            slope = Mathf.Pow(slope, steepPower);
            float blendLow = heightBlendLow(samplePoint);
            float blendHigh = heightBlendHigh(samplePoint);
            float midPoint = (Vector3.Distance(samplePoint, planetOrigin) - planetRadius) / (blendHighStart + blendLowEnd);
            Vector3 n = new Vector3(Math.Abs(sampleNormal.x), Math.Abs(sampleNormal.y), Math.Abs(sampleNormal.z));

            // determine major axis (in x; yz are following axis)
            Vector3 ma = (n.x > n.y && n.x > n.z) ? new Vector3(0, 1, 2) :
                       (n.y > n.z) ? new Vector3(1, 2, 0) :
                                              new Vector3(2, 0, 1);
            // determine minor axis (in x; yz are following axis)
            Vector3 mi = (n.x < n.y && n.x < n.z) ? new Vector3(0, 1, 2) :
                       (n.y < n.z) ? new Vector3(1, 2, 0) :
                                              new Vector3(2, 0, 1);
            // determine median axis (in x;  yz are following axis)
            Vector3 me = (new Vector3(3, 3, 3)) - mi - ma;
            float UVx = (float)((samplePoint[(int)ma.y] * _ST.x - floatUV[(int)ma.y]) );
            float UVy = (float)((samplePoint[(int)ma.z] * _ST.y - floatUV[(int)ma.z]) );
            float UVMex = (float)((samplePoint[(int)me.y] * _ST.x - floatUV[(int)me.y]) );
            float UVMey = (float)((samplePoint[(int)me.z] * _ST.y - floatUV[(int)me.z]) );
            Color x = Color.black;
            Color y = Color.black;
            if (tex.isReadable)
            {
                x = tex.GetPixelBilinear(UVx, UVy, 0);
                y = tex.GetPixelBilinear(UVMex, UVMey, 0);
            }
            else { Debug.Log("<color=#ffffff>Displacement is not readable!"); }
            
            Vector2 w = new Vector2(n[(int)ma.x], n[(int)me.x]);
            // make local support
            w = (w - new Vector2(0.5773f, 0.5773f)) / new Vector2((1.0f - 0.5773f), (1.0f - 0.5773f));
            w = new Vector2(Mathf.Clamp(w.x, 0, 1), Mathf.Clamp(w.y, 0, 1));
            // shape transition
            w = new Vector2(Mathf.Pow(w.x, (float)(1 / 8.0)), Mathf.Pow(w.y, (float)(1 / 8.0)));
            //Replace the 1 above with Strength
            // blend and return
            Vector4 finalCol = (x * w.x + y * w.y) / (w.x + w.y);

            plane.transform.up = sampleNormal;

            float finalColLow = finalCol.x;
            float finalColMid = finalCol.y;
            float finalColHigh = finalCol.z;
            float finalColSteep = finalCol.w;

            //Debug.DrawLine(transform.position, 100 * -sampleNormal, Color.red, 1000f, true);

            float displacement = LerpSurfaceColor(finalColLow, finalColMid, finalColHigh, finalColSteep, midPoint, slope, blendLow, blendHigh);

            //return finalCol.x;
            return displacement;
        }
        Vector4 LerpSurfaceNormal(Vector4 low, Vector4 mid, Vector4 high, Vector4 steep, float midPoint, float slope, float blendLow, float blendHigh)
        {
            Vector4 col;
            if (midPoint < 0.5)
            {
                col = Vector4.Lerp(low, mid, 1 - blendLow);
            }
            else
            {
                col = Vector4.Lerp(mid, high, blendHigh);
            }
            col = Vector4.Lerp(col, steep, 1 - slope);
            return col;
        }
        float heightBlendLow(Vector3 worldPos)
        {
            float terrainHeight = (float)FlightGlobals.getAltitudeAtPos(worldPos);//Vector3.Distance(worldPos, planetOrigin) - planetRadius;
            
            float blendLow = Mathf.Clamp((terrainHeight - blendLowEnd) / (blendLowStart - blendLowEnd), 0, 1);
            return blendLow;
        }
        float heightBlendHigh(Vector3 worldPos)
        {
            float terrainHeight = (float)FlightGlobals.getAltitudeAtPos(worldPos);

            float blendHigh = Mathf.Clamp((terrainHeight - blendHighStart) / (blendHighEnd - blendHighStart), 0, 1);
            return blendHigh;
        }
        
        float LerpSurfaceColor(float low, float mid, float high, float steep, float midPoint, float slope, float blendLow, float blendHigh)
        {
            float col;
            if (midPoint < 0.5)
            {
                col = Mathf.Lerp(low, mid, 1 - blendLow);
            }
            else
            {
                
                col = Mathf.Lerp(mid, high, blendHigh);
            }
            col = Mathf.Lerp(col, steep, 1 - slope);
            return col;
        }
    }
    
}

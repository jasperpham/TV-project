using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TVMountLab;

[InitializeOnLoad]
public static class MountLabBuilder {
    const string Root="Assets/TVMountLab";
    static Material yellow,black,steel,screen,wood,wall,teal,orange,white,dark;
    static Mesh cylinder;
    static MountLabBuilder(){EditorApplication.delayCall+=AutoBuild;}
    static void AutoBuild(){if(!File.Exists(Root+"/BuildComplete.txt")&&!EditorApplication.isPlayingOrWillChangePlaymode)Build();}
    static Material Mat(string name,Color c,float metallic=0,float smooth=.35f){
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.color=c;m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);AssetDatabase.CreateAsset(m,Root+"/Materials/"+name+".mat");return m;
    }
    static GameObject Group(string n,Transform parent=null){var g=new GameObject(n);if(parent)g.transform.SetParent(parent,false);return g;}
    static GameObject Box(string n,Transform p,Vector3 pos,Vector3 scale,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;return g;}
    // A flat-shaded 12-sided cylinder along local Z; meshes are stored as assets.
    static Mesh Cylinder(){var m=new Mesh();m.name="Faceted12Cylinder";var vs=new System.Collections.Generic.List<Vector3>();var ts=new System.Collections.Generic.List<int>();
        Action<Vector3,Vector3,Vector3> tri=(a,b,c)=>{int k=vs.Count;vs.Add(a);vs.Add(b);vs.Add(c);ts.Add(k);ts.Add(k+1);ts.Add(k+2);};
        for(int i=0;i<12;i++){float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;var v0=new Vector3(Mathf.Cos(a)*.5f,Mathf.Sin(a)*.5f,-.5f);var v1=new Vector3(Mathf.Cos(b)*.5f,Mathf.Sin(b)*.5f,-.5f);var v2=v0+Vector3.forward;var v3=v1+Vector3.forward;tri(v0,v1,v2);tri(v1,v3,v2);tri(Vector3.back*.5f,v1,v0);tri(Vector3.forward*.5f,v2,v3);}
        m.SetVertices(vs);m.SetTriangles(ts,0);m.RecalculateNormals();m.RecalculateBounds();AssetDatabase.CreateAsset(m,Root+"/Meshes/FacetedCylinder.asset");return m;}
    static GameObject Cyl(string n,Transform p,Vector3 pos,Vector3 scale,Material mat){var g=Group(n,p);g.AddComponent<MeshFilter>().sharedMesh=cylinder;g.AddComponent<MeshRenderer>().sharedMaterial=mat;g.transform.localPosition=pos;g.transform.localScale=scale;return g;}
    static TextMesh Text(string n,Transform p,string value,Vector3 pos,float size,Material unused=null){var g=Group(n,p);g.transform.localPosition=pos;var t=g.AddComponent<TextMesh>();t.text=value;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(.83f,.91f,.92f);return t;}
    static GameObject Save(GameObject g,string n){var prefab=PrefabUtility.SaveAsPrefabAsset(g,Root+"/Prefabs/"+n+".prefab");UnityEngine.Object.DestroyImmediate(g);return prefab;}
    static GameObject Spawn(GameObject prefab,Transform p,Vector3 pos){var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,p);g.transform.localPosition=pos;return g;}
    static GameObject Rail(){var g=Group("Horizontal Wall Rail");var t=g.transform;
        Box("Upper flange",t,new Vector3(0,.042f,0),new Vector3(.84f,.018f,.035f),black);Box("Lower flange",t,new Vector3(0,-.042f,0),new Vector3(.84f,.018f,.035f),black);
        for(int i=0;i<13;i++)Box("Slot web "+i,t,new Vector3(-.408f+i*.068f,0,.009f),new Vector3(.026f,.066f,.017f),black);
        return Save(g,"WallRail");}
    static GameObject Plate(){var g=Group("Center Pivot Plate");Box("Pressed steel plate",g.transform,Vector3.zero,new Vector3(.18f,.23f,.026f),black);Cyl("Pivot cap",g.transform,new Vector3(0,0,-.027f),new Vector3(.063f,.063f,.025f),steel);return Save(g,"CenterPlate");}
    static GameObject Arm(){var g=Group("Diagonal Mount Arm");Box("Arm",g.transform,Vector3.zero,new Vector3(.052f,.45f,.022f),black);for(int i=0;i<5;i++)Cyl("Adjustment hole "+i,g.transform,new Vector3(0,-.16f+i*.08f,-.013f),new Vector3(.017f,.017f,.004f),steel);return Save(g,"DiagonalArm");}
    static GameObject Screw(){var g=Group("M6 Mounting Fastener");Cyl("Thread core",g.transform,new Vector3(0,0,.026f),new Vector3(.008f,.008f,.052f),steel);for(int i=0;i<9;i++)Cyl("Thread "+i,g.transform,new Vector3(0,0,.004f+i*.005f),new Vector3(.011f,.011f,.002f),steel);Cyl("Washer",g.transform,Vector3.zero,new Vector3(.027f,.027f,.003f),steel);Cyl("Hex socket head",g.transform,new Vector3(0,0,-.006f),new Vector3(.017f,.017f,.01f),black);Box("Drive recess",g.transform,new Vector3(0,0,-.0115f),new Vector3(.009f,.002f,.001f),steel);g.AddComponent<BoxCollider>().size=new Vector3(.028f,.028f,.07f);return Save(g,"MountingScrew");}
    static GameObject Drill(){var g=Group("Yellow Black Cordless Drill");var t=g.transform;
        Box("Motor housing",t,new Vector3(0,.115f,.045f),new Vector3(.082f,.09f,.16f),yellow);
        Cyl("Rear motor cap",t,new Vector3(0,.115f,-.043f),new Vector3(.078f,.078f,.025f),black);
        Cyl("Torque collar",t,new Vector3(0,.115f,.143f),new Vector3(.07f,.07f,.048f),black);
        Cyl("Metal chuck",t,new Vector3(0,.115f,.179f),new Vector3(.04f,.04f,.028f),steel);
        var bit=Group("Spinning Bit",t);bit.transform.localPosition=new Vector3(0,.115f,.205f);
        Cyl("Driver bit",bit.transform,new Vector3(0,0,.025f),new Vector3(.007f,.007f,.068f),steel);
        var tip=Group("Bit Tip",bit.transform);tip.transform.localPosition=new Vector3(0,0,.059f);
        var grip=Box("Rubber grip",t,new Vector3(0,.025f,-.004f),new Vector3(.053f,.13f,.057f),black);grip.transform.localRotation=Quaternion.Euler(-12,0,0);
        Box("Grip yellow spine",t,new Vector3(0,.022f,-.033f),new Vector3(.043f,.105f,.012f),yellow);
        Box("Trigger",t,new Vector3(0,.063f,.035f),new Vector3(.027f,.032f,.02f),black);
        Box("Battery pack",t,new Vector3(0,-.065f,-.01f),new Vector3(.088f,.045f,.102f),black);Box("Battery yellow latch",t,new Vector3(0,-.044f,.036f),new Vector3(.056f,.012f,.012f),yellow);
        for(int i=0;i<4;i++)for(int s=-1;s<=1;s+=2)Box("Cooling vent",t,new Vector3(s*.042f,.108f,-.005f+i*.018f),new Vector3(.002f,.034f,.006f),black);
        for(int i=0;i<5;i++)Box("Grip texture",t,new Vector3(.027f,-.02f+i*.016f,0),new Vector3(.002f,.004f,.036f),steel);
        return Save(g,"ControllerDrill");}
    static GameObject TV(){var g=Group("55 inch Flat Screen TV");var t=g.transform;Box("Slim bezel",t,Vector3.zero,new Vector3(1.23f,.71f,.044f),black);Box("Glass display",t,new Vector3(0,.006f,-.024f),new Vector3(1.19f,.663f,.005f),screen);Box("Rear electronics",t,new Vector3(0,0,.032f),new Vector3(.52f,.3f,.027f),black);Box("Status LED",t,new Vector3(.55f,-.34f,-.026f),new Vector3(.012f,.003f,.003f),teal);
        Text("Screen typography",t,"MOUNT / LAB\n55  /  TRAINING DISPLAY",new Vector3(0,0,-.029f),.0035f);
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)Cyl("VESA 400 x 200",t,new Vector3(x*.2f,y*.1f,.047f),new Vector3(.012f,.012f,.003f),steel);
        return Save(g,"FlatScreenTV");}
    static GameObject Console(){var g=Group("Compact Fireplace Media Console");var t=g.transform;
        Box("Oak top",t,new Vector3(0,.66f,0),new Vector3(1.6f,.07f,.43f),wood);Box("Oak plinth",t,new Vector3(0,.095f,0),new Vector3(1.53f,.07f,.4f),wood);
        for(int s=-1;s<=1;s+=2){Box("Side panel",t,new Vector3(s*.73f,.37f,0),new Vector3(.06f,.5f,.38f),wood);Box("Leg",t,new Vector3(s*.65f,.035f,0),new Vector3(.09f,.07f,.28f),black);Box("Cabinet",t,new Vector3(s*.55f,.37f,0),new Vector3(.27f,.48f,.35f),wood);Box("Handle",t,new Vector3(s*.55f,.47f,-.181f),new Vector3(.09f,.014f,.018f),black);}
        Box("Firebox frame",t,new Vector3(0,.37f,-.015f),new Vector3(.81f,.49f,.34f),black);Box("Firebox dark glass",t,new Vector3(0,.37f,-.19f),new Vector3(.72f,.39f,.018f),dark);
        for(int i=0;i<6;i++){var log=Cyl("Faceted log",t,new Vector3(-.27f+i*.105f,.245f,-.212f),new Vector3(.046f,.046f,.14f),wood);log.transform.localRotation=Quaternion.Euler(0,35,70);var flame=Box("Stylized electric flame",t,new Vector3(-.27f+i*.105f,.31f+(i%2)*.025f,-.218f),new Vector3(.035f,.1f+(i%3)*.025f,.014f),orange);flame.transform.localRotation=Quaternion.Euler(0,0,20-i*8);}
        return Save(g,"FireplaceConsole");}
    [MenuItem("Tools/TV Mount Lab/Build Training Assets")]
    public static void Build(){
      try{
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Root+"/Scenes/MountPractice.unity")){Debug.Log("TV Mount Lab already built; existing assets preserved.");return;}
        foreach(var d in new[]{"Materials","Meshes","Prefabs","Scenes","Audio"})Directory.CreateDirectory(Root+"/"+d);
        AssetDatabase.Refresh();
        yellow=Mat("Safety Yellow",new Color(1,.68f,.035f));black=Mat("Graphite",new Color(.035f,.045f,.055f),.3f);steel=Mat("Brushed Steel",new Color(.45f,.53f,.59f),.85f);screen=Mat("Midnight Glass",new Color(.018f,.085f,.12f),.35f,.85f);wood=Mat("Warm Oak",new Color(.36f,.19f,.085f));wall=Mat("Warm Plaster",new Color(.66f,.69f,.65f));teal=Mat("Ready Teal",new Color(.03f,.78f,.68f));orange=Mat("Amber Ember",new Color(1,.26f,.025f));white=Mat("Chalk",new Color(.88f,.87f,.79f));dark=Mat("Firebox",new Color(.025f,.016f,.018f));
        orange.EnableKeyword("_EMISSION");orange.SetColor("_EmissionColor",new Color(2,.35f,.02f));
        cylinder=Cylinder();var rail=Rail();var plate=Plate();var arm=Arm();var screw=Screw();var drill=Drill();var tv=TV();var console=Console();
        var screws=Group("Left Controller Screw Supply");for(int i=0;i<4;i++){var s=Spawn(screw,screws.transform,new Vector3((i-1.5f)*.034f,.035f,.07f));s.transform.localRotation=Quaternion.Euler(-15,0,(i-1.5f)*8);}var supply=Save(screws,"ControllerScrewSupply");
        var mount=Group("Modular X Arm TV Mount");Spawn(rail,mount.transform,new Vector3(0,.2f,0));Spawn(rail,mount.transform,new Vector3(0,-.2f,0));Spawn(plate,mount.transform,new Vector3(0,0,-.07f));
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2){var a=Spawn(arm,mount.transform,new Vector3(x*.145f,y*.097f,-.05f));a.transform.localRotation=Quaternion.Euler(0,0,-x*y*56);Cyl("VESA end pivot",mount.transform,new Vector3(x*.3f,y*.2f,-.07f),new Vector3(.029f,.029f,.025f),steel);}
        var mountPrefab=Save(mount,"ModularTVMount");
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var room=Group("01 / Mount Lab Environment");Box("Floor",room.transform,new Vector3(0,-.075f,.7f),new Vector3(6,.15f,6),white);Box("Mounting wall",room.transform,new Vector3(0,1.55f,2.64f),new Vector3(5.8f,3.1f,.16f),wall);Box("Skirting",room.transform,new Vector3(0,.065f,2.54f),new Vector3(5.8f,.13f,.035f),white);
        Box("Feature wall",room.transform,new Vector3(0,1.52f,2.546f),new Vector3(2.05f,2.85f,.016f),dark);
        for(int i=0;i<17;i++)Box("Oak wall batten",room.transform,new Vector3(-.96f+i*.12f,1.52f,2.52f),new Vector3(.018f,2.85f,.025f),wood);
        Spawn(console,room.transform,new Vector3(0,0,2.25f));
        var installed=Spawn(mountPrefab,null,new Vector3(0,1.65f,2.46f));installed.name="02 / Modular Mount - 2 Rails + 4 X Arms";
        var trainer=Group("05 / Training Flow").AddComponent<MountTraining>();trainer.fasteners=new Transform[4];trainer.indicators=new Renderer[4];trainer.readyMaterial=teal;trainer.doneMaterial=yellow;
        int k=0;for(int y=1;y>=-1;y-=2)for(int x=-1;x<=1;x+=2){var s=Spawn(screw,installed.transform,new Vector3(x*.34f,y*.2f,-.105f));s.name="Fastener "+(k+1);s.SetActive(false);trainer.fasteners[k]=s.transform;var halo=Cyl("Target "+(k+1),installed.transform,new Vector3(x*.34f,y*.2f,-.03f),new Vector3(.043f,.043f,.008f),teal);trainer.indicators[k]=halo.GetComponent<Renderer>();k++;}
        var television=Spawn(tv,null,new Vector3(-1.6f,1.17f,1.94f));television.name="03 / TV Staging";trainer.television=television.transform;
        Box("TV staging pedestal",room.transform,new Vector3(-1.6f,.47f,1.96f),new Vector3(1.38f,.86f,.4f),dark);
        var dock=Group("TV Dock - front of mounted bracket",installed.transform);dock.transform.localPosition=new Vector3(0,0,-.17f);trainer.tvDock=dock.transform;
        var rig=Group("04 / XR Training Rig");var camera=Group("Head / Main Camera",rig.transform);camera.tag="MainCamera";camera.transform.localPosition=new Vector3(0,1.65f,-1.3f);var cam=camera.AddComponent<Camera>();cam.nearClipPlane=.025f;camera.AddComponent<AudioListener>();trainer.head=camera.transform;
        var left=Group("Left Controller / Screw Hand",rig.transform);left.transform.localPosition=new Vector3(-.24f,1.12f,.15f);var right=Group("Right Controller / Drill Hand",rig.transform);right.transform.localPosition=new Vector3(.25f,1.09f,.12f);trainer.leftHand=left.transform;trainer.rightHand=right.transform;Spawn(supply,left.transform,Vector3.zero);var held=Spawn(drill,right.transform,Vector3.zero);trainer.bit=held.transform.Find("Spinning Bit");trainer.drillTip=trainer.bit.Find("Bit Tip");
        PrefabUtility.SaveAsPrefabAsset(rig,Root+"/Prefabs/ControllerTrackedRig.prefab");
        var audio=held.AddComponent<AudioSource>();audio.loop=true;audio.spatialBlend=1;audio.volume=.12f;audio.playOnAwake=false;audio.minDistance=.2f;audio.maxDistance=4;
        var clip=AudioClip.Create("DrillMotor",22050,1,22050,false);var samples=new float[22050];for(int i=0;i<samples.Length;i++)samples[i]=Mathf.Sin(i*2*Mathf.PI*110/22050)*.22f+Mathf.Sin(i*2*Mathf.PI*330/22050)*.07f;
        // PCM WAV is a portable imported audio asset.
        using(var f=new BinaryWriter(File.Create(Root+"/Audio/DrillMotor.wav"))){f.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));f.Write(36+samples.Length*2);f.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));f.Write(16);f.Write((short)1);f.Write((short)1);f.Write(22050);f.Write(44100);f.Write((short)2);f.Write((short)16);f.Write(System.Text.Encoding.ASCII.GetBytes("data"));f.Write(samples.Length*2);foreach(float s in samples)f.Write((short)(s*32767));}UnityEngine.Object.DestroyImmediate(clip);AssetDatabase.ImportAsset(Root+"/Audio/DrillMotor.wav");audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/DrillMotor.wav");trainer.motor=audio;
        var board=Group("06 / Instruction Board");Box("Board backing",board.transform,new Vector3(1.83f,1.58f,2.47f),new Vector3(1.25f,1.35f,.04f),dark);
        Text("Title",board.transform,"MOUNT / LAB",new Vector3(1.83f,2.1f,2.44f),.006f);
        Text("Subtitle",board.transform,"VR INSTALLATION PRACTICE",new Vector3(1.83f,1.94f,2.44f),.0026f);
        trainer.instructions=Text("Live instructions",board.transform,"02 / PLACE FASTENER 1 OF 4\nMove left hand to halo + trigger\nDesktop: E  Place   |   R  Reset",new Vector3(1.83f,1.62f,2.44f),.0025f);
        Text("Scope note",board.transform,"SIMULATED MECHANICS\nFollow mount + fireplace manufacturer\nclearances for real installations.",new Vector3(1.83f,1.13f,2.44f),.0022f);
        Text("Wall heading",room.transform,"01 / SECURE THE WALL MOUNT",new Vector3(0,2.48f,2.47f),.0035f);
        Text("TV staging label",room.transform,"04 / SCREEN STAGING",new Vector3(-1.6f,.67f,1.746f),.003f);
        var light=Group("Key Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(45,-30,0);light.shadows=LightShadows.Soft;
        var fill=Group("Soft Fill").AddComponent<Light>();fill.type=LightType.Point;fill.range=7;fill.intensity=9;fill.transform.position=new Vector3(0,2,-.8f);
        RenderSettings.ambientLight=new Color(.36f,.4f,.44f);cam.backgroundColor=new Color(.1f,.14f,.17f);cam.clearFlags=CameraClearFlags.SolidColor;
        EditorSceneManager.SaveScene(scene,Root+"/Scenes/MountPractice.unity");
        // Keep the user's original scene loaded; avoid multiple active cameras when testing.
        if(previous.IsValid()){
            if(previous.isDirty)EditorSceneManager.SaveScene(previous,Root+"/Scenes/OriginalSceneSnapshot.unity",true);
            EditorSceneManager.CloseScene(previous,false);
        }
        Selection.activeGameObject=installed;if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,1.4f,2.3f),Quaternion.Euler(8,0,0),4.5f);
        AssetDatabase.SaveAssets();File.WriteAllText(Root+"/BuildComplete.txt","Built "+DateTime.Now+"\nOriginal SampleScene asset preserved.\n");AssetDatabase.Refresh();Debug.Log("TV MOUNT LAB BUILD COMPLETE - separate prefabs and practice scene created.");
      }catch(Exception e){Debug.LogException(e);}
    }
}

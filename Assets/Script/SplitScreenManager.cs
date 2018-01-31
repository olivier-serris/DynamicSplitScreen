﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoronoiSplitScreen
{
    using Voronoi2;
    public class SplitScreenManager : MonoBehaviour
    {
        static SplitScreenManager singleton;
        List<SplitScreenCamera> splitCameraList;
        [SerializeField] GameObject[] targets;
        [SerializeField] Material debugMat;
        [SerializeField] Material stencilDrawer;
        [SerializeField] Material[] stencilDrawerTab;
        Color[] debugColor = new Color[6] { Color.white, Color.black,Color.magenta, Color.red,  Color.cyan,Color.grey};
        List <Mesh> polyMaskList;
        public GameObject[] Targets
        {
            get{return targets;}

            set{targets = value;}
        }

        static public SplitScreenManager Singleton
        {
            get
            {
                if (singleton == null)
                    Debug.LogError("SplitScreenManager Missing");
                return singleton;
            }
        }

        public void ComputeWorldBounds(out Bounds worldBounds, out Vector3[] voronoiSitePos)
        {
            worldBounds = new Bounds();
            voronoiSitePos = new Vector3[targets.Length];
            Vector2 newExtents = Vector2.zero;
            Vector2 playerAveragePosition = Vector2.zero;
            for (int i = 0; i < targets.Length; i++)
            {
                playerAveragePosition += new Vector2(targets[i].transform.position.x, targets[i].transform.position.y);
            }
            worldBounds.center = playerAveragePosition /= targets.Length;
            for (int i = 0; i < targets.Length; i++)
            {
                voronoiSitePos[i] = (targets[i].transform.position - worldBounds.center);
                voronoiSitePos[i].z = 0;
                newExtents = worldBounds.extents;
                float test = Camera.main.aspect;
                test = 1.0f*Screen.currentResolution.width / Screen.currentResolution.height;
                test = 1.0f * Screen.width / Screen.height;

                if (newExtents.x < Mathf.Abs(voronoiSitePos[i].x))
                {
                    newExtents.x = Mathf.Abs(voronoiSitePos[i].x);
                    newExtents.y = newExtents.x /Camera.main.aspect;
                }
                if (newExtents.y < Mathf.Abs(voronoiSitePos[i].y))
                {
                    newExtents.y = Mathf.Abs(voronoiSitePos[i].y);
                    newExtents.x = newExtents.y * Camera.main.aspect;
                }
                    // Min camera Size for player.
                worldBounds.extents = newExtents;
            }
            worldBounds.extents = newExtents;
        }
        public void ResizeStencilPolygoneMesh(Bounds worldBounds ,Vector3[] targetVoronoiPos)
        {
            Voronoi voronoi = new Voronoi(0.1f);
            

            double[] x = new double[targets.Length];
            double[] y = new double[targets.Length];


            
            boundsGizmo = worldBounds;
            for (int i = 0; i < targetVoronoiPos.Length; i++)
            {
                x[i] = targetVoronoiPos[i].x;
                y[i] = targetVoronoiPos[i].y;
            }
            List<GraphEdge> edges = voronoi.generateVoronoi(x, y, -worldBounds.extents.x, worldBounds.extents.x,
                                                                  -worldBounds.extents.y, worldBounds.extents.y);
            if (edges == null || edges.Count <= 0)
            {
                Debug.Log("Error with Voronoi edge List");
                return;
            }
            polyMaskList.Clear();
            for (int i = 0; i < targetVoronoiPos.Length; i++)
                polyMaskList.Add(MeshHelper.GetPolygon(i, edges, targetVoronoiPos, worldBounds));
        }
        public void Split(SplitScreenCamera splitOne,int idOne,int idTwo)
        {
            
        }

        public void Awake()
        {
            singleton = this;
            polyMaskList = new List<Mesh>();
            splitCameraList = new List<SplitScreenCamera>();
            stencilDrawerTab = new Material[3];
            //stencilDrawerTab[0] = new Material(Shader.Find("Stencils/StencilDrawer"));
            //stencilDrawerTab[0].SetFloat("_StencilMask", 0);
            //stencilDrawerTab[1] = new Material(Shader.Find("Stencils/StencilDrawer"));
            //stencilDrawerTab[1].SetFloat("_StencilMask", 1);


            for (int i = 0; i < stencilDrawerTab.Length; i++)
            {
                stencilDrawerTab[i] = new Material(Shader.Find("Stencils/StencilDrawer"));
                stencilDrawerTab[i].SetFloat("_StencilMask", i);
            }

        }
        public void Start()
        {
            for (int i = 0; i < transform.childCount;i++)
            {
                SplitScreenCamera splitCamera = transform.GetChild(i).GetComponentInChildren<SplitScreenCamera>();
                splitCamera.Init(targets[i].transform,i);
                splitCameraList.Add(splitCamera);
            }
        }

        public void Update()
        {
            Bounds worldBounds;
            Vector3[] targetVoronoiPos;
            ComputeWorldBounds(out worldBounds, out targetVoronoiPos);
            ResizeStencilPolygoneMesh(worldBounds, targetVoronoiPos);

            for (int i = 0; i < splitCameraList.Count; i++)
            {
                splitCameraList[i].targetVoronoiScreenPos.x = targetVoronoiPos[i].x / worldBounds.extents.x;
                splitCameraList[i].targetVoronoiScreenPos.y = targetVoronoiPos[i].y / worldBounds.extents.y;
            }

            for (int i= 0; i < polyMaskList.Count;i++)
            {
                //MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                //propertyBlock.SetColor("color", debugColor[i]);
                //Graphics.DrawMesh(polyMaskList[i], Vector3.zero, Quaternion.identity, debugMat, 0, Camera.main, 0, propertyBlock);
                //MaterialPropertyBlock propertyBlock2 = new MaterialPropertyBlock();
                //propertyBlock2.SetFloat("_StencilMask", i);
                //Graphics.DrawMesh(polyMaskList[i], Vector3.zero, Quaternion.identity, stencilDrawer, 0, Camera.main, 0, propertyBlock2);
                //Graphics.DrawMesh(polyMaskList[i], Vector3.zero, Quaternion.identity, stencilDrawer,0);
                Graphics.DrawMesh(polyMaskList[i], Camera.main.transform.position+ Camera.main.transform.forward, Quaternion.identity, stencilDrawerTab[i],0,Camera.main,0);
            }
        }
        Bounds boundsGizmo;
        public void OnDrawGizmos()
        {
            if (boundsGizmo!=null)
            {
                Gizmos.color = Color.grey;
                Gizmos.DrawLine(boundsGizmo.min, boundsGizmo.min + new Vector3(boundsGizmo.size.x, 0, 0));
                Gizmos.DrawLine(boundsGizmo.max, boundsGizmo.max - new Vector3(boundsGizmo.size.x, 0, 0));

                Gizmos.DrawLine(boundsGizmo.min, boundsGizmo.min + new Vector3(0, boundsGizmo.size.y, 0));
                Gizmos.DrawLine(boundsGizmo.max, boundsGizmo.max - new Vector3(0, boundsGizmo.size.y, 0));

                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(boundsGizmo.center, 0.2f);
            }
        }
    }

    
}


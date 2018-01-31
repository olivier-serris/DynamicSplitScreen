﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoronoiSplitScreen
{
    public enum CameraMode {singleTarget,multipleTarget }
    public class SplitScreenCamera : MonoBehaviour
    {
        new Camera camera;
        Transform primaryTarget;
        List <Transform> targetInDeadZone = new List<Transform>();
        public Vector2 targetVoronoiScreenPos;
        int id;

            // Command Buffer
        Mesh quadPerso;
        private CommandBuffer cmdBufferStencil;
        private CommandBuffer cmdBufferLastCamera;
        private Material stencilRenderer;
        static private RenderTexture lastCameraRender = null;
        static private RenderTargetIdentifier lastCameraRenderId;

        #region getterSetters
        public int ID
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        #region CommandBuffer
        void OnStart()
        {
            if (quadPerso == null)
                quadPerso = MeshHelper.GetQuad();
            if (stencilRenderer== null)
            {
                stencilRenderer = new Material(Shader.Find("Hidden/StencilRenderer"));
                stencilRenderer.SetFloat("_StencilMask", id);
            }

            if (lastCameraRender == null)
            {
                lastCameraRender = new RenderTexture(Screen.width, Screen.height, 16);
                lastCameraRenderId = new RenderTargetIdentifier(lastCameraRender);
            }
            // Second command buffer test : 
            if (cmdBufferLastCamera == null)
            {
                cmdBufferLastCamera = new CommandBuffer();
                cmdBufferLastCamera.name = "cmdBufferLastCamera";
                RenderTexture active = RenderTexture.active;
                cmdBufferLastCamera.Blit(BuiltinRenderTextureType.CurrentActive, lastCameraRenderId);
                cmdBufferLastCamera.SetRenderTarget(active);
                camera.AddCommandBuffer(CameraEvent.AfterEverything, cmdBufferLastCamera);
            }
            if (cmdBufferStencil == null)
            {
                cmdBufferStencil = new CommandBuffer();
                cmdBufferStencil.name = "Camera Stencil Mask";

                // Je prends une texture temporaire(MyCameraRd) qui est liée à la propriété de mon shader.
                int MyCameraRdID = Shader.PropertyToID("MyCameraRd");
                cmdBufferStencil.GetTemporaryRT(MyCameraRdID, -1, -1, 0, FilterMode.Bilinear);
                // je blit la cameraTarget dans cette MyCameraRd.
                // donc texID deviens la renderTarget
                cmdBufferStencil.Blit(BuiltinRenderTextureType.CameraTarget, MyCameraRdID);
                // la renderTarget devient CameraTarget
                cmdBufferStencil.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                if (id == 0)
                    cmdBufferStencil.ClearRenderTarget(false, true, Color.black);
                else
                    cmdBufferStencil.Blit(lastCameraRenderId, BuiltinRenderTextureType.CameraTarget);
                // je draw dans MyCameraRd dans  MyCameraRd qui est de nouveau la renderTarget
                cmdBufferStencil.DrawMesh(quadPerso, Matrix4x4.identity, stencilRenderer, 0, 0);

                // je draw de renderTarget vers CameraRD avec le tempBuffer Comme texture.
                camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, cmdBufferStencil);

                  
            }
            
        }
        void OnDisable()
        {
            if (cmdBufferStencil != null)
                camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, cmdBufferStencil);
            cmdBufferStencil = null;
        }
        #endregion


        public void Init(Transform _primaryTarget, int _Id)
        {
            camera = GetComponent<Camera>();
            id = _Id;
            primaryTarget = _primaryTarget;
            OnStart();
        }
        public void UpdateTargets()
        {
            GameObject[] target = SplitScreenManager.Singleton.Targets;
            for (int i = 0; i < target.Length; i++)
            {
                Vector3 viewPortPos = camera.WorldToViewportPoint(target[i].transform.position);
                //viewPortPos = (viewPortPos *2)- Vector3.one;

                bool onScreen = viewPortPos.x > 0.25f && viewPortPos.y > 0.25f
                && viewPortPos.x < 0.75f && viewPortPos.y < 0.75f;
                if (onScreen)
                {
                    if (!targetInDeadZone.Contains(target[i].transform))
                    {
                        targetInDeadZone.Add(target[i].transform);
                        Merge();
                    }
                }
                else
                {
                    if (targetInDeadZone.Contains(target[i].transform))
                    {
                        targetInDeadZone.Remove(target[i].transform);
                        Split();
                    }
                }

            }
            // je parcours les joueurs.
            // j'ajoute les joueurs qui sont dans ma deadZone. 
            // Si un joueur n'est plus dans ma deadZone. (=> stuff
            // Si un joueur est ajouté à ma deadZone (=>suff
        }
        public void Merge()
        {
                // sur 2 camera il n'en existe plus qu'une et elle est repositionnée.
        }
        public void Split()
        {
                // On passe à 2 camera chacun ayant leur target.
        }
        public void FollowOnePlayer()
        {
            Vector3 playerOffSet = camera.ViewportToWorldPoint((targetVoronoiScreenPos + Vector2.one) * 0.5f) - transform.position;
            Vector3 cameraPos = primaryTarget.transform.position - playerOffSet;
            transform.position = new Vector3(cameraPos.x, cameraPos.y, transform.position.z);
        }
        public void FollowMultiplePlayer()
        {
            Vector3 playerAverage = Vector3.zero;
            for (int i = 0; i < targetInDeadZone.Count; i++)
                playerAverage += targetInDeadZone[i].position;
            transform.position = new Vector3(playerAverage.x, playerAverage.y, transform.position.z);
        }
        public void Update()
        {
            UpdateTargets();
            //FollowOnePlayer();
            //FollowMultiplePlayer();
            if (targetInDeadZone.Count == 1)
                FollowOnePlayer();
            else
                FollowMultiplePlayer();
        }
    }

}


/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Oculus.Interaction
{
    public struct TubePoint
    {
        public Vector3 position;
        public Vector3 direction;
        public float relativeLength;
    }

    /// <summary>
    /// Creates and renders a tube mesh from sequence of points.
    /// </summary>
    public class TubeRenderer : MonoBehaviour
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct VertexLayout
        {
            public Vector3 pos;
            public Color32 color;
            public Vector2 uv;
        }

        [SerializeField]
        private MeshFilter _filter;
        [SerializeField]
        private MeshRenderer _renderer;
        [SerializeField]
        private int _divisions = 6;

        [SerializeField]
        private float _radius = 0.005f;
        public float Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                _radius = value;
            }
        }

        [SerializeField]
        private Gradient _gradient;
        public Gradient Gradient
        {
            get
            {
                return _gradient;
            }
            set
            {
                _gradient = value;
            }
        }

        [SerializeField]
        private Color _tint = Color.white;
        public Color Tint
        {
            get
            {
                return _tint;
            }
            set
            {
                _tint = value;
            }
        }
        [SerializeField, Range(0f, 1f)]
        private float _progressFade = 0.2f;
        public float ProgressFade
        {
            get
            {
                return _progressFade;
            }
            set
            {
                _progressFade = value;
            }
        }

        [SerializeField, Range(0f, 1f)]
        private float _endFadeThresold = 0.2f;
        public float EndFadeThresold
        {
            get
            {
                return _endFadeThresold;
            }
            set
            {
                _endFadeThresold = value;
            }
        }

        [SerializeField]
        private bool _mirrorTexture;
        public bool MirrorTexture
        {
            get
            {
                return _mirrorTexture;
            }
            set
            {
                _mirrorTexture = value;
            }
        }

        public float Progress { get; set; } = 0f;

        private VertexAttributeDescriptor[] _dataLayout;
        private NativeArray<VertexLayout> _vertsData;
        private VertexLayout _layout = new VertexLayout();
        private Mesh _mesh;
        private int[] _tris;
        private int _initializedSteps = -1;

        #region Editor events

        protected virtual void Reset()
        {
            _filter = this.GetComponent<MeshFilter>();
            _renderer = this.GetComponent<MeshRenderer>();
        }

        #endregion

        protected virtual void OnDestroy()
        {
            if (_initializedSteps != -1)
            {
                _vertsData.Dispose();
            }
        }

        protected virtual void OnEnable()
        {
            _renderer.enabled = true;
        }

        protected virtual void OnDisable()
        {
            _renderer.enabled = false;
        }


        public void RenderTube(TubePoint[] points)
        {
            int steps = points.Length;
            if (steps != _initializedSteps)
            {
                InitializeMeshData(steps);
                _initializedSteps = steps;
            }
            UpdateMeshData(points, _divisions, _radius, _tint);
            _renderer.enabled = true;
        }

        public void Hide()
        {
            _renderer.enabled = false;
        }

        private void InitializeMeshData(int steps)
        {
            _dataLayout = new VertexAttributeDescriptor[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            };

            int vertsCount = SetVertexCount(steps, _divisions);
            _vertsData = new NativeArray<VertexLayout>(vertsCount, Allocator.Persistent);
            _mesh = new Mesh();
            _mesh.SetVertexBufferParams(vertsCount, _dataLayout);
            _mesh.SetTriangles(_tris, 0);
            _filter.mesh = _mesh;
        }

        private void UpdateMeshData(TubePoint[] points, int divisions, float width, Color tint)
        {
            int steps = points.Length;
            Quaternion rotation = Quaternion.identity;
            float endFade = points[steps - 1].relativeLength - EndFadeThresold;
            for (int i = 0; i < steps; i++)
            {
                Vector3 point = points[i].position;
                float progress = points[i].relativeLength;
                Color color = Gradient.Evaluate(progress) * tint;
                if (i / (steps - 1f) < Progress)
                {
                    color.a *= ProgressFade;
                }
                else if (progress > endFade)
                {
                    float dif = 1f - ((progress - endFade) / EndFadeThresold);
                    color.a *= dif;
                }
                _layout.color = color;

                if (i < steps - 1)
                {
                    rotation = Quaternion.LookRotation(points[i].direction);
                }

                for (int j = 0; j <= divisions; j++)
                {
                    float radius = 2 * Mathf.PI * j / divisions;
                    Vector3 circle = new Vector3(Mathf.Sin(radius), Mathf.Cos(radius), 0);
                    Vector3 normal = rotation * circle;

                    _layout.pos = point + normal * width;
                    if (_mirrorTexture)
                    {
                        float x = (j / (float)divisions) * 2f;
                        if (j >= divisions * 0.5f)
                        {
                            x = 2 - x;
                        }
                        _layout.uv = new Vector2(x, progress);
                    }
                    else
                    {
                        _layout.uv = new Vector2(j / (float)divisions, progress);
                    }
                    int vertIndex = i * (divisions + 1) + j;
                    _vertsData[vertIndex] = _layout;
                }
            }

            _mesh.bounds = new Bounds(
                (points[0].position + points[steps - 1].position) * 0.5f,
                points[steps - 1].position - points[0].position);
            _mesh.SetVertexBufferData(_vertsData, 0, 0, _vertsData.Length, 0, MeshUpdateFlags.DontRecalculateBounds);
        }

        private int SetVertexCount(int positionCount, int divisions)
        {
            int vertsPerPosition = divisions + 1;
            int vertCount = positionCount * vertsPerPosition;

            int tubeTriangles = (positionCount - 1) * divisions * 6;
            int capTriangles = (divisions - 2) * 3;
            _tris = new int[tubeTriangles + capTriangles * 2];

            // handle triangulation
            for (int i = 0; i < positionCount - 1; i++)
            {
                // add faces
                for (int j = 0; j < divisions; j++)
                {
                    int vert0 = i * vertsPerPosition + j;
                    int vert1 = (i + 1) * vertsPerPosition + j;
                    int t = (i * divisions + j) * 6;
                    _tris[t] = vert0;
                    _tris[t + 1] = _tris[t + 4] = vert1;
                    _tris[t + 2] = _tris[t + 3] = vert0 + 1;
                    _tris[t + 5] = vert1 + 1;
                }
            }

            // triangulate the ends
            Cap(tubeTriangles, 0, divisions - 1, true);
            Cap(tubeTriangles + capTriangles, vertCount - divisions, vertCount - 1);

            void Cap(int t, int firstVert, int lastVert, bool clockwise = false)
            {
                for (int i = firstVert + 1; i < lastVert; i++)
                {
                    _tris[t++] = firstVert;
                    _tris[t++] = clockwise ? i : i + 1;
                    _tris[t++] = clockwise ? i + 1 : i;
                }
            }

            return vertCount;
        }

        #region Inject
        public void InjectAllTubeRenderer(MeshFilter filter,
            MeshRenderer renderer, int divisions)
        {
            InjectFilter(filter);
            InjectRenderer(renderer);
            InjectDivisions(divisions);
        }
        public void InjectFilter(MeshFilter filter)
        {
            _filter = filter;
        }
        public void InjectRenderer(MeshRenderer renderer)
        {
            _renderer = renderer;
        }
        public void InjectDivisions(int divisions)
        {
            _divisions = divisions;
        }

        #endregion
    }
}

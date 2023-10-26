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

using System;
using Meta.Conduit;
using Meta.WitAi;
using UnityEngine;

namespace Meta.Voice.Samples.Chess
{
    public class ChessBoardController : MonoBehaviour
    {
        public GameObject letters;
        public GameObject numbers;
        public GameObject chessPiece;
        public UnityEngine.UI.Text errorText;
        private Vector3 _targetPosition;

        void Awake()
        {
            _targetPosition = chessPiece.transform.position;
        }

        // Update is called once per frame
        void Update()
        {
            chessPiece.transform.position = Vector3.Lerp(chessPiece.transform.position, _targetPosition, Time.deltaTime);
        }

        public enum ChessBoardLetter
        {
            A,
            B,
            C,
            D,
            E,
            F,
            G,
            H
        }
        [MatchIntent("MoveChessPiece")]
        public void MoveChessPiece(ChessBoardLetter letter, int number)
        {
            Debug.Log("Move chess piece to " + letter + number);

            _targetPosition = new Vector3(letters.transform.GetChild((int)letter).position.x, _targetPosition.y,
                numbers.transform.GetChild(number - 1).position.z);

        }

        [HandleEntityResolutionFailure]
        public void OnHandleEntityResolutionFailure(string intent , Exception ex)
        {
            Debug.Log("Failed to resolve parameter for intent " + intent + " with error " + ex.Message);
            errorText.text = ex.Message;
        }

    }
}

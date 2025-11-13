using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UniRx;
using UniState;
using UnityEngine;

namespace TestUniState
{

    /// <summary>
    /// thay doi main_Model.state de xem state dc thay doi ntn
    /// </summary>
    public class TestState : MonoBehaviour
    {
        StateMachine stateMachine;
        public Main_Model main_Model;

        [System.Serializable]
        public class Main_Model
        {
            public int StateGame;
            public int IDCard;
        }


        public class DefaultResolver : ITypeResolver
        {
            public object Resolve(Type type)
            {
                return Activator.CreateInstance(type);
            }
        }

        void Start()
        {

            main_Model = new Main_Model();
            stateMachine = new StateMachine();
            stateMachine.SetResolver(new DefaultResolver());

            this.ObserveEveryValueChanged(_ => main_Model.StateGame).Subscribe(delegate
            {
                Main_Model Main_Model = new();
                switch (main_Model.StateGame)
                {
                    case 0:
                        stateMachine.Execute<State_CHia_bai, Main_Model>(Main_Model, destroyCancellationToken).Forget();
                        break;
                    case 1:
                        stateMachine.Execute<State_User_Play, Main_Model>(Main_Model, destroyCancellationToken).Forget();
                        break;
                }

            }).AddTo(destroyCancellationToken);

        }


        public class State_CHia_bai : StateBase<Main_Model>
        {
            public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
            {
                Debug.Log($"Execute State_CHia_bai");
                await UniTask.WaitForSeconds(2);
                // return Transition.GoTo<State_User_Play, State_User_Play_Model>(new());
                return Transition.GoToExit();
                // return Transition.GoTo
            }

            public override UniTask Exit(CancellationToken token)
            {
                Debug.Log($"Exit State_CHia_bai");
                return base.Exit(token);
            }
        }

        public class State_User_Play : StateBase<Main_Model>
        {


            public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
            {
                Debug.Log($"Execute State_User_Play");
                await UniTask.WaitForSeconds(1);

                return Transition.GoToExit();
            }


            public override UniTask Exit(CancellationToken token)
            {
                Debug.Log($"Exit State_User_Play");
                return base.Exit(token);
            }
        }

    }
}

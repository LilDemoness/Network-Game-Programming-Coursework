using UnityEngine;
using VContainer.Unity;

namespace Gameplay.GameState
{
    public enum GameState
    {
        MainMenu,
        GameLobby,
        InGameplay,
        PostGameScreen,
    }

    /// <summary>
    ///     A special component that represents a discrete game state and its dependencies. <br/>
    ///     The special feature it offers is that it provies some guarentees that only one such GameState will be running at a time.
    /// </summary>
    /// <remarks>
    /// Q: What is the relationship between a GameState and a Scene?
    /// A: There is a 1-to-many relationship between states and scenes. That is, every scene corresponds to exactly one state,
    ///    but a single state can exist in multiple scenes.
    /// Q: How do state transitions happen?
    /// A: They are driven implicitly by calling NetworkManager.SceneManager.LoadScene in server code. This is
    ///    important, because if state transitions were driven separately from scene transitions, then states that cared what
    ///    scene they ran in would need to carefully synchronize their logic to scene loads.
    /// Q: How many GameStateBehaviours are there?
    /// A: Exactly one on the server and one on the client (on the host a server and client GameStateBehaviour will run concurrently, as
    ///    with other networked prefabs).
    /// Q: If these are MonoBehaviours, how do you have a single state that persists across multiple scenes?
    /// A: Set your Persists property to true. If you transition to another scene that has the same gamestate, the
    ///    current GameState object will live on, and the version in the new scene will auto-destruct to make room for it.
    ///
    /// Important Note: We assume that every Scene has a GameState object. If not, then it's possible that a Persisting game state
    /// will outlast its lifetime (as there is no successor state to clean it up).
    /// </remarks>
    public abstract class GameStateBehaviour : LifetimeScope
    {
        /// <summary>
        ///     Does this GameState persist across multiple scenes?
        /// </summary>
        public virtual bool Persists => false;


        /// <summary>
        ///     What GameState this represents. Server and Client specialisations of a state should always return the same enum.
        /// </summary>
        public abstract GameState ActiveState { get; }


        /// <summary>
        ///     This is the single active GameState object. There can only be one.
        /// </summary>
        private static GameObject s_activeStateGO;


        protected override void Awake()
        {
            base.Awake();

            if (Parent != null)
            {
                Parent.Container.Inject(this);
            }
        }
        protected virtual void Start()
        {
            if (s_activeStateGO != null)
            {
                if (s_activeStateGO == gameObject)
                    return; // We're already the active state object, so we don't need to do anything.
                // We are not the active state object. Clean up the previous one.

                // On the host, this might return either the client or server version, but it won't matter which.
                //  We only care about the type and its Persist state.
                GameStateBehaviour previousState = s_activeStateGO.GetComponent<GameStateBehaviour>();

                if (previousState.Persists && previousState.ActiveState == ActiveState)
                {
                    // We need to make way for the DontDestroyOnLoad state that already exists.
                    Destroy(this.gameObject);
                    return;
                }

                // Otherwise, the old state will be getting cleared.
                //  Either it wasn't persistant or we are a different kind of state. In either case, we're replacing it.
                Destroy(s_activeStateGO);
            }

            // Put ourselves as the Active State GameObject.
            s_activeStateGO = this.gameObject;
            if (Persists)
            {
                DontDestroyOnLoad(this.gameObject);
            }
        }

        protected override void OnDestroy()
        {
            if (!Persists)
            {
                s_activeStateGO = null;
            }
        }
    }
}
using UnityEngine;
using System;
using System.Collections;
using Robotech.TBS.AI;

namespace Robotech.TBS.Core
{
    /// <summary>
    /// Manages turn progression and phase transitions for single-player skirmish gameplay.
    /// Handles the Player -> AI -> Next Turn cycle.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        /// <summary>
        /// Represents the current phase of gameplay.
        /// </summary>
        public enum TurnPhase
        {
            /// <summary>Player's turn to act.</summary>
            Player,
            /// <summary>AI's turn to act.</summary>
            AI
        }

        /// <summary>
        /// Gets the current phase of the turn (Player or AI).
        /// </summary>
        public TurnPhase CurrentPhase { get; private set; } = TurnPhase.Player;

        /// <summary>
        /// Gets the current turn number (1-based indexing).
        /// </summary>
        public int TurnNumber { get; private set; } = 1;

        /// <summary>
        /// Event triggered when a new turn begins.
        /// Passes the new turn number as parameter.
        /// </summary>
        public static event Action<int> OnTurnStarted;

        /// <summary>
        /// Event triggered when a turn ends (after both Player and AI phases complete).
        /// Passes the ending turn number as parameter.
        /// </summary>
        public static event Action<int> OnTurnEnded;

        /// <summary>
        /// Event triggered when the phase changes (Player <-> AI).
        /// Passes the new phase as parameter.
        /// </summary>
        public static event Action<TurnPhase> OnPhaseChanged;

        /// <summary>
        /// AI thinking delay in seconds. Adjust for desired AI response time.
        /// </summary>
        [SerializeField]
        private float aiThinkingDelay = 0.5f;

        /// <summary>
        /// Whether to wait for AIController to signal completion before ending AI phase.
        /// </summary>
        [SerializeField]
        private bool waitForAIController = true;

        private bool aiPhaseComplete = false;

        void Start()
        {
            // Subscribe to AI completion event
            AIController.OnAIPhaseComplete += OnAIComplete;

            // Initialize the first turn
            OnTurnStarted?.Invoke(TurnNumber);
            OnPhaseChanged?.Invoke(CurrentPhase);
        }

        void OnDestroy()
        {
            AIController.OnAIPhaseComplete -= OnAIComplete;
        }

        private void OnAIComplete()
        {
            aiPhaseComplete = true;
        }

        /// <summary>
        /// Ends the current phase and transitions to the next phase or turn.
        /// If in Player phase, transitions to AI phase.
        /// If in AI phase, advances to the next turn.
        /// </summary>
        public void EndPhase()
        {
            if (CurrentPhase == TurnPhase.Player)
            {
                // Transition from Player to AI phase
                StartCoroutine(ProcessAIPhase());
            }
            else
            {
                // End of AI phase -> advance to next turn
                EndTurn();
            }
        }

        /// <summary>
        /// Processes the AI phase using a coroutine to avoid recursion.
        /// Allows for delayed AI actions and smooth UI transitions.
        /// </summary>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private IEnumerator ProcessAIPhase()
        {
            // Transition to AI phase
            CurrentPhase = TurnPhase.AI;
            aiPhaseComplete = false;
            OnPhaseChanged?.Invoke(CurrentPhase);

            // Allow UI to update before AI acts
            yield return null;

            // Wait for AIController to complete (if enabled)
            if (waitForAIController)
            {
                // Give AIController time to receive the phase change event and start processing
                yield return new WaitForSeconds(0.1f);

                // Wait for AI to signal completion
                float timeout = 30f; // Maximum wait time
                float elapsed = 0f;
                while (!aiPhaseComplete && elapsed < timeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }

                if (!aiPhaseComplete)
                {
                    Debug.LogWarning("[TurnManager] AI phase timed out!");
                }
            }
            else
            {
                // Fallback: just use the thinking delay
                yield return new WaitForSeconds(aiThinkingDelay);
            }

            // End the AI phase (non-recursively)
            EndPhase();
        }

        /// <summary>
        /// Ends the current turn and begins the next turn.
        /// Increments turn number and resets phase to Player.
        /// </summary>
        private void EndTurn()
        {
            OnTurnEnded?.Invoke(TurnNumber);
            TurnNumber++;
            CurrentPhase = TurnPhase.Player;
            OnTurnStarted?.Invoke(TurnNumber);
            OnPhaseChanged?.Invoke(CurrentPhase);
        }

        /// <summary>
        /// Resets the turn manager to initial state.
        /// Useful for restarting matches or scenarios.
        /// </summary>
        public void ResetTurnManager()
        {
            TurnNumber = 1;
            CurrentPhase = TurnPhase.Player;
            OnTurnStarted?.Invoke(TurnNumber);
            OnPhaseChanged?.Invoke(CurrentPhase);
        }
    }
}

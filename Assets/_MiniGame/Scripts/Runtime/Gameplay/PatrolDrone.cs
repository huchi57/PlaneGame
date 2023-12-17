using System;
using UnityEngine;

namespace UrbanFox.MiniGame
{
    public class PatrolDrone : MonoBehaviour
    {
        [Serializable]
        public enum State
        {
            Patrol,
            Chase,
            GiveUp,
            CaughtPlayer
        }

        [SerializeField, NonEditable]
        private State m_currentState = State.Patrol;

        [Header("Interpolation Speed")]
        [SerializeField]
        private float m_positionLerpSpeed = 5;

        [SerializeField]
        private bool m_enableRotationSlerp = true;

        [SerializeField, EnableIf(nameof(m_enableRotationSlerp), true)]
        private float m_rotationSlerpSpeed = 5;

        [Header("Patrol State Options")]
        [SerializeField, Range(0, 1)]
        private float m_percentageOnPatrolCurve;

        [SerializeField]
        private bool m_autoMove = true;

        [SerializeField, Indent, EnableIf(nameof(m_autoMove), true)]
        private float m_percentageIncreaseRate = 0.1f;

        [Header("Patrol Points Curve")]
        [SerializeField]
        private BezierCurveType m_curveType;

        [SerializeField, Required, Info("Patrol Points will be automatically allocated from this object's children.")]
        private Transform m_patrolPointsParentObject;

        [SerializeField, NonEditable]
        private Transform[] m_patrolPoints;

        [Header("Chase State Options")]
        [SerializeField]
        private float m_targetChaseSpeed = 1;

        [SerializeField]
        private float m_chaseSpeedAcceleration = 1;

        [SerializeField]
        private DroneLight m_droneLight;

        [Header("Debug")]
        [SerializeField]
        private bool m_drawGizmos;

        private Vector3[] m_patrolPointsWorldSpace;

        private Vector3 m_lastFramePosition;
        private Vector3 m_deltaPosition;

        private Transform m_currentChasingTarget;
        private float m_currentChasingSpeed;

        public void StartChasingTarget(Transform target)
        {
            m_currentChasingTarget = target;
            m_currentState = State.Chase;
            if (m_droneLight)
            {
                m_droneLight.StartTracking(target);
            }
        }

        private void Awake()
        {
            m_lastFramePosition = transform.position;
            m_patrolPointsWorldSpace = new Vector3[m_patrolPoints.IsNullOrEmpty() ? 0 : m_patrolPoints.Length + 1];
        }

        private void Start()
        {
            if (!m_patrolPoints.IsNullOrEmpty())
            {
                var detachedParent = new GameObject($"{name}_PatrolPoints").transform;
                detachedParent.MoveObjectToScene(gameObject.scene);
                foreach (var point in m_patrolPoints)
                {
                    if (point)
                    {
                        point.SetParent(detachedParent);
                    }
                }
            }
        }

        private void Update()
        {
            switch (m_currentState)
            {
                case State.Patrol:
                    UpdatePatrolState();
                    break;
                case State.Chase:
                    UpdateChaseState();
                    break;
                case State.GiveUp:
                    break;
                case State.CaughtPlayer:
                    break;
                default:
                    break;
            }
            if (m_enableRotationSlerp)
            {
                var targetRotation = Quaternion.LookRotation(m_deltaPosition.IsApproximately(Vector3.zero) ? transform.forward : m_deltaPosition);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_rotationSlerpSpeed * Time.deltaTime);
            }
        }

        private void UpdatePatrolState()
        {
            m_deltaPosition = transform.position - m_lastFramePosition;
            m_lastFramePosition = transform.position;
            UpdatePatrolPointsData();
            if (m_autoMove)
            {
                m_percentageOnPatrolCurve += m_percentageIncreaseRate * Time.deltaTime;
                if (m_percentageOnPatrolCurve > 1f)
                {
                    m_percentageOnPatrolCurve -= 1f;
                }
            }
            var targetPosition = BezierCurve.GetPointOnCurve(m_patrolPointsWorldSpace, m_percentageOnPatrolCurve, m_curveType);
            transform.position = Vector3.Lerp(transform.position, targetPosition, m_positionLerpSpeed * Time.deltaTime);
        }

        private void UpdatePatrolPointsData()
        {
            if (m_patrolPoints.IsNullOrEmpty())
            {
                return;
            }
            for (int i = 0; i < m_patrolPoints.Length; i++)
            {
                if (m_patrolPoints[i])
                {
                    m_patrolPointsWorldSpace[i] = m_patrolPoints[i].position;
                }
            }
            if (m_patrolPoints[0])
            {
                m_patrolPointsWorldSpace[m_patrolPointsWorldSpace.Length - 1] = m_patrolPoints[0].position;
            }
        }

        private void UpdateChaseState()
        {
            if (m_currentChasingTarget)
            {
                m_currentChasingSpeed = Mathf.Lerp(m_currentChasingSpeed, m_targetChaseSpeed, m_chaseSpeedAcceleration * Time.deltaTime);
                var targetPosition = transform.position + m_currentChasingSpeed * Time.deltaTime * (m_currentChasingTarget.position - transform.position).normalized;
                transform.position = targetPosition;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying || !m_patrolPointsParentObject)
            {
                return;
            }
            m_patrolPoints = new Transform[m_patrolPointsParentObject.childCount];
            for (int i = 0; i < m_patrolPointsParentObject.childCount; i++)
            {
                m_patrolPoints[i] = m_patrolPointsParentObject.GetChild(i);
            }
            m_patrolPointsWorldSpace = new Vector3[m_patrolPoints.IsNullOrEmpty() ? 0 : m_patrolPoints.Length + 1];
            if (!m_patrolPoints.IsNullOrEmpty())
            {
                for (int i = 0; i < m_patrolPoints.Length; i++)
                {
                    if (m_patrolPoints[i])
                    {
                        m_patrolPoints[i].name = $"{name}_PatrolPoint_{i}";
                    }
                }
            }
            UpdatePatrolPointsData();
        }

        private void OnDrawGizmos()
        {
            if (!m_drawGizmos)
            {
                return;
            }

            var currentlySelectedObjects = UnityEditor.Selection.transforms;
            if (currentlySelectedObjects.IsNullOrEmpty())
            {
                return;
            }

            foreach (var item in currentlySelectedObjects)
            {
                if (item && (item.IsChildOf(transform) || item == transform))
                {
                    OnValidate();
                    BezierCurve.DrawCurve(m_patrolPointsWorldSpace, m_percentageOnPatrolCurve, m_curveType);
                    return;
                }
            }
        }
#endif
    }
}

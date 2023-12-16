using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

namespace UrbanFox.MiniGame
{
    public class DebugManager : RuntimeManager<DebugManager>
    {
        [Header("Components")]
        [SerializeField, Required] private CinemachineVirtualCamera m_cinemachineVirtualCamera;
        [SerializeField, Required] private Text m_debugText;
        [SerializeField] private GameObject m_debugGraph;

        [Header("Display Options")]
        [SerializeField] private bool m_enableDebug;

        private void Start()
        {
            if (m_debugGraph)
            {
                m_debugGraph.SetActive(m_enableDebug);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                m_enableDebug = !m_enableDebug;
                if (m_debugGraph)
                {
                    m_debugGraph.SetActive(m_enableDebug);
                }
            }
            if (m_debugGraph.activeSelf)
            {
                m_debugText.text = GetDebugContent();
            }
        }

        private string GetDebugContent()
        {
            var content = new StringBuilder();
            if (GameManager.IsInstanceExist && ApplicationBuildData.Instance)
            {
                content.AppendLine($"{ApplicationBuildData.Instance.GetBuildInfoText()}");
                content.AppendLine($"Game State: {GameManager.Instance.CurrentGameState}");
                content.AppendLine($"FOV: {m_cinemachineVirtualCamera.m_Lens.FieldOfView:F2}");

                // TODO: Add more texts
            }
            return content.ToString();
        }
    }
}

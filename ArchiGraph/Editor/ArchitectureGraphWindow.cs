using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArchiGraph
{
    public class ArchitectureGraphWindow : EditorWindow
    {
        private ArchitectureGraphView _graphView;
        private string _selectedFolder = "Assets";

        [MenuItem("Window/ArchiGraph")]
        public static void Open()
        {
            var window = GetWindow<ArchitectureGraphWindow>();
            window.titleContent = new GUIContent("ArchiGraph");
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
        }

        private void OnDisable()
        {
            if (_graphView != null)
                rootVisualElement.Remove(_graphView);
        }

        private void ConstructGraphView()
        {
            _graphView = new ArchitectureGraphView();
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();

            var selectFolder = new ToolbarButton(() =>
            {
                string folder = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(folder) && folder.StartsWith(Application.dataPath))
                {
                    _selectedFolder = "Assets" + folder.Substring(Application.dataPath.Length);
                    _graphView.RemoveAllNodes();
                    _graphView.CreateNodesFromFolder(_selectedFolder);

                    _graphView.ForceRebuildEdges();
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Folder", "Folder must be inside the project", "OK");
                }
            })
            { text = "Select Folder" };

            var showAll = new ToolbarButton(() =>
            {
                _graphView.RemoveAllNodes();
                _graphView.CreateNodesFromFolder("Assets");

                _graphView.ForceRebuildEdges();
            })
            { text = "Show All" };

            var addProperties = new ToolbarButton(() =>
            {
                _graphView.SpawnInspectorNode();
            })
            { text = "Add Properties" };

            var removeAll = new ToolbarButton(() =>
            {
                _graphView.RemoveAllNodes();
            })
            { text = "Remove All" };

            toolbar.Add(selectFolder);
            toolbar.Add(showAll);
            toolbar.Add(addProperties);
            toolbar.Add(removeAll);

            rootVisualElement.Add(toolbar);
        }
    }
}

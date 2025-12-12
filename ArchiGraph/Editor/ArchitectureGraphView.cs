using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArchiGraph
{
    public class ArchitectureGraphView : GraphView
    {
        private readonly Dictionary<Type, ArchitectureNodeView> _nodeByType = new();
        private readonly Dictionary<string, Type> _targetLookup = new();
        private readonly List<Group> _namespaceGroups = new();

        private ArchitectureInspectorNodeView _inspectorNode;
        private string _currentFolder = "Assets";

        private readonly Dictionary<Type, List<Type>> _depsByType = new();

        public ArchitectureGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ContentZoomer());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            style.flexGrow = 1;
        }

        public void ForceRebuildEdges()
        {
            RemoveAllEdgesOnly();
            CreateEdges();
        }

        public void SpawnInspectorNode()
        {
            if (_inspectorNode != null && nodes.Contains(_inspectorNode))
                return;

            _inspectorNode = new ArchitectureInspectorNodeView();
            AddElement(_inspectorNode);

            _inspectorNode.OnApply = (offsetChanged) =>
            {
                if (!string.IsNullOrEmpty(_currentFolder))
                {
                    RemoveAllNodes();
                    CreateNodesFromFolder(_currentFolder);

                    if (ArchitectureGraphSettings.ShowDependency)
                        CreateEdges();

                    if (offsetChanged)
                        FrameAllScheduled();
                }
            };

            float width = 260f;
            float height = 200f;
            float margin = 50f;

            schedule.Execute(() =>
            {
                Vector2 screenTopRight = new Vector2(layout.width - margin, margin);
                Vector2 graphTopRight = contentViewContainer.WorldToLocal(screenTopRight);

                var rect = new Rect(
                    graphTopRight.x - width,
                    graphTopRight.y,
                    width,
                    height
                );

                _inspectorNode.SetPosition(rect);
            });
        }

        public void CreateNodesFromFolder(string folder)
        {
            _currentFolder = folder;
            RemoveAllNodes();

            var typesInFolder = ClassScanner.GetTypesFromFolder(folder);
            if (typesInFolder.Count == 0)
            {
                FrameAllScheduled();
                return;
            }

            var typeSet = new HashSet<Type>(typesInFolder);
            var projectTypes = TypeScanner.GetProjectTypes();

            _depsByType.Clear();
            var outCount = new Dictionary<Type, int>();
            var inCount = new Dictionary<Type, int>();

            foreach (var type in typesInFolder)
            {
                var deps = DependencyScanner.GetClassDependencies(type, projectTypes)
                    .Where(d => typeSet.Contains(d))
                    .ToList();

                _depsByType[type] = deps;

                outCount[type] = deps.Count;

                foreach (var dep in deps)
                {
                    if (!inCount.TryGetValue(dep, out var c))
                        c = 0;
                    inCount[dep] = c + 1;
                }
            }

            var foundTypes = typesInFolder
                .Select(t => (type: t, nsKey: t.Namespace ?? "__global__"))
                .ToList();

            var groups = foundTypes
                .GroupBy(t => t.nsKey)
                .OrderBy(g => g.Key == "__global__" ? 0 : 1)
                .ThenBy(g => g.Key)
                .ToList();

            float offset = ArchitectureGraphSettings.Offset;

            float startX = 100f;
            float startY = 100f;

            float nodeWidth = 260f;

            var groupInfos = new List<GroupInfo>();

            foreach (var g in groups)
            {
                var types = g.Select(t => t.type).ToList();
                int count = types.Count;
                if (count == 0)
                    continue;

                int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
                int rows = Mathf.CeilToInt(count / (float)cols);

                float maxNodeHeight = 0f;

                foreach (var t in types)
                {
                    var deps = _depsByType.TryGetValue(t, out var list) ? list : new List<Type>();
                    float h = 120f + deps.Count * 26f;
                    if (h > maxNodeHeight)
                        maxNodeHeight = h;
                }

                float cellWidth = nodeWidth + offset;
                float cellHeight = maxNodeHeight + offset;

                float groupWidth = cols * cellWidth;
                float groupHeight = rows * cellHeight;

                groupInfos.Add(new GroupInfo
                {
                    NamespaceKey = g.Key,
                    Types = types,
                    Cols = cols,
                    Rows = rows,
                    CellWidth = cellWidth,
                    CellHeight = cellHeight,
                    GroupWidth = groupWidth,
                    GroupHeight = groupHeight
                });
            }

            if (groupInfos.Count == 0)
            {
                FrameAllScheduled();
                return;
            }

            int groupCount = groupInfos.Count;
            int groupCols = Mathf.CeilToInt(Mathf.Sqrt(groupCount));
            int groupRows = Mathf.CeilToInt(groupCount / (float)groupCols);

            float maxGroupWidth = groupInfos.Max(info => info.GroupWidth);
            float maxGroupHeight = groupInfos.Max(info => info.GroupHeight);

            float cellGroupWidth = maxGroupWidth + offset * 2f;
            float cellGroupHeight = maxGroupHeight + offset * 2f;

            int groupIndex = 0;

            foreach (var info in groupInfos)
            {
                int gcol = groupIndex % groupCols;
                int grow = groupIndex / groupCols;

                float groupOriginX = startX + gcol * cellGroupWidth;
                float groupOriginY = startY + grow * cellGroupHeight;

                var sortedTypes = info.Types
                    .OrderBy(t => GetRank(t, inCount, outCount))
                    .ThenBy(t => t.Name)
                    .ToList();

                for (int i = 0; i < sortedTypes.Count; i++)
                {
                    var type = sortedTypes[i];

                    int c = i % info.Cols;
                    int r = i / info.Cols;

                    float cellX = groupOriginX + c * info.CellWidth;
                    float cellY = groupOriginY + r * info.CellHeight;

                    var deps = _depsByType.TryGetValue(type, out var list) ? list : new List<Type>();
                    float nodeHeight = 120f + deps.Count * 26f;

                    float x = cellX + (info.CellWidth - nodeWidth) * 0.5f;
                    float y = cellY + (info.CellHeight - nodeHeight) * 0.5f;

                    var node = CreateNodeForType(type, new Vector2(x, y));
                    _nodeByType[type] = node;
                }

                groupIndex++;
            }

            BuildNamespaceGroups();
            BuildLookupTable();
        }

        private int GetRank(Type type, Dictionary<Type, int> inCount, Dictionary<Type, int> outCount)
        {
            inCount.TryGetValue(type, out var inc);
            outCount.TryGetValue(type, out var outc);

            bool hasIn = inc > 0;
            bool hasOut = outc > 0;

            if (!hasIn && hasOut) return 0;
            if (hasIn && hasOut) return 1;
            if (hasIn && !hasOut) return 2;

            return 1;
        }

        private ArchitectureNodeView CreateNodeForType(Type type, Vector2 pos)
        {
            List<Type> deps = DependencyScanner.GetClassDependencies(type, TypeScanner.GetProjectTypes());

            var node = new ArchitectureNodeView(type.Name, type.IsInterface);
            node.SetIsInterface(type.IsInterface);

            float height = 120 + deps.Count * 26;
            node.SetPosition(new Rect(pos, new Vector2(260, height)));

            node.OnNodeSelected = () =>
            {
                if (_inspectorNode != null)
                    _inspectorNode.ShowNode(type, deps);
            };

            node.OnNodeDeselected = () =>
            {
                if (_inspectorNode != null)
                    _inspectorNode.ClearInspector();
            };

            foreach (var dep in deps)
            {
                var port = node.InstantiatePort(
                    Orientation.Horizontal,
                    Direction.Output,
                    Port.Capacity.Single,
                    typeof(bool)
                );

                bool isInterfaceDep = dep.IsInterface;

                port.userData = isInterfaceDep;

                port.portName = dep.Name;
                port.portColor = isInterfaceDep
                    ? ArchitectureGraphSettings.InterfaceColor
                    : ArchitectureGraphSettings.ClassColor;

                var label = port.Q<Label>();
                if (label != null)
                {
                    label.style.color = port.portColor;
                    label.EnableInClassList("unity-disabled", false);
                }

                node.outputContainer.Add(port);
            }

            AddElement(node);

            node.RefreshExpandedState();
            node.RefreshPorts();
            node.RefreshColors?.Invoke();

            return node;
        }

        private void BuildLookupTable()
        {
            _targetLookup.Clear();

            foreach (var type in _nodeByType.Keys)
            {
                string name = type.Name;
                if (!_targetLookup.ContainsKey(name))
                    _targetLookup.Add(name, type);
            }
        }

        private void CreateEdges()
        {
            foreach (var (type, node) in _nodeByType)
            {
                foreach (var port in node.outputContainer.Children().OfType<Port>())
                {
                    string target = port.portName;

                    if (_targetLookup.TryGetValue(target, out var t) &&
                        _nodeByType.TryGetValue(t, out var targetNode))
                    {
                        var inputPort = targetNode.inputContainer.Q<Port>();
                        if (inputPort != null)
                        {
                            var edge = port.ConnectTo(inputPort);
                            AddElement(edge);
                        }
                    }
                }
            }
        }

        private void BuildNamespaceGroups()
        {
            foreach (var g in _namespaceGroups)
                RemoveElement(g);

            _namespaceGroups.Clear();

            var map = new Dictionary<string, List<ArchitectureNodeView>>();

            foreach (var (type, node) in _nodeByType)
            {
                string nsKey = type.Namespace ?? "__global__";

                if (!map.TryGetValue(nsKey, out var list))
                    map[nsKey] = list = new List<ArchitectureNodeView>();

                list.Add(node);
            }

            foreach (var (nsKey, nodes) in map)
            {
                string title = nsKey == "__global__" ? "Global" : nsKey;

                var group = new Group { title = title };
                AddElement(group);

                foreach (var n in nodes)
                    group.AddElement(n);

                _namespaceGroups.Add(group);
            }
        }

        public void RemoveAllNodes()
        {
            foreach (var e in edges.ToList())
                RemoveElement(e);

            foreach (var n in nodes.ToList())
                RemoveElement(n);

            foreach (var g in _namespaceGroups)
                RemoveElement(g);

            _namespaceGroups.Clear();
            _nodeByType.Clear();
            _targetLookup.Clear();
            _depsByType.Clear();
            _inspectorNode = null;
        }

        public void RemoveAllEdgesOnly()
        {
            foreach (var e in edges.ToList())
                RemoveElement(e);
        }

        public void FrameAllScheduled()
        {
            schedule.Execute(() =>
            {
                schedule.Execute(() =>
                {
                    FrameAll();
                }).ExecuteLater(1);
            }).ExecuteLater(1);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter adapter)
        {
            var list = new List<Port>();

            ports.ForEach(port =>
            {
                if (port == startPort) return;
                if (port.node == startPort.node) return;
                if (port.direction == startPort.direction) return;

                list.Add(port);
            });

            return list;
        }

        private class GroupInfo
        {
            public string NamespaceKey;
            public List<Type> Types;
            public int Cols;
            public int Rows;
            public float CellWidth;
            public float CellHeight;
            public float GroupWidth;
            public float GroupHeight;
        }
    }
}

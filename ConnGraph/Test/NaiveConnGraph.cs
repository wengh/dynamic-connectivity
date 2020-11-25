using System;
using System.Collections.Generic;
using System.Linq;

namespace Connectivity.test
{
    public class NaiveConnGraph : IConnGraph
    {
        private class VertexInfo
        {
            private bool Equals(VertexInfo other)
            {
                return Equals(vertex, other.vertex);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((VertexInfo) obj);
            }

            public readonly ConnVertex vertex;
            public readonly HashSet<VertexInfo> edges = new HashSet<VertexInfo>();
            public object aug;
            public long iteration = 0;
            public bool direction = false;

            public override int GetHashCode()
            {
                return vertex.GetHashCode();
            }

            public VertexInfo(ConnVertex vertex)
            {
                this.vertex = vertex ?? throw new ArgumentNullException();
            }
        }

        private readonly IAugmentation _augmentation;
        private Dictionary<ConnVertex, VertexInfo> _info = new Dictionary<ConnVertex, VertexInfo>();

        private VertexInfo EnsureInfo(ConnVertex vertex)
        {
            if (_info.TryGetValue(vertex, out var existingInfo))
                return existingInfo;

            var newInfo = new VertexInfo(vertex);
            _info[vertex] = newInfo;
            return newInfo;
        }

        public NaiveConnGraph()
        {
        }

        public NaiveConnGraph(IAugmentation augmentation)
        {
            _augmentation = augmentation;
        }

        public bool AddEdge(ConnVertex connVertex1, ConnVertex connVertex2)
        {
            var info1 = EnsureInfo(connVertex1);
            var info2 = EnsureInfo(connVertex2);
            info1.edges.Add(info2);
            return info2.edges.Add(info1);
        }

        private bool RemoveEdge(VertexInfo u, VertexInfo v)
        {
            bool result = u.edges.Remove(v);
            if (u.edges.Count == 0)
                u.iteration = 0;

            return result;
        }

        public bool RemoveEdge(ConnVertex vertex1, ConnVertex vertex2)
        {
            var info1 = EnsureInfo(vertex1);
            var info2 = EnsureInfo(vertex2);
            RemoveEdge(info1, info2);
            return RemoveEdge(info2, info1);
        }

        private long _iteration = 0;

        public bool IsConnected(ConnVertex vertex1, ConnVertex vertex2)
        {
            long iteration = ++_iteration;

            var info1 = EnsureInfo(vertex1);
            var info2 = EnsureInfo(vertex2);

            info1.iteration = iteration;
            info2.iteration = iteration;

            var queue = new Queue<VertexInfo>(new[] {info1, info2});
            while (queue.Count > 0)
            {
                var vert = queue.Dequeue();
                foreach (var next in vert.edges)
                {
                    if (next.iteration == iteration)
                    {
                        if (next.direction != vert.direction)
                            return true;
                        continue;
                    }
                    next.iteration = iteration;
                    queue.Enqueue(next);
                }
            }
            return false;
        }

        public ICollection<ConnVertex> AdjacentVertices(ConnVertex vertex)
        {
            if (_info.TryGetValue(vertex, out var info))
            {
                return info.edges.Select(x => x.vertex).ToArray();
            }
            else
            {
                return new ConnVertex[0];
            }
        }

        public object SetVertexAugmentation(ConnVertex connVertex, object vertexAugmentation)
        {
            var info = EnsureInfo(connVertex);
            var old = info.aug;
            info.aug = vertexAugmentation;
            return old;
        }

        public object RemoveVertexAugmentation(ConnVertex connVertex)
        {
            var info = EnsureInfo(connVertex);
            var old = info.aug;
            info.aug = null;
            return old;
        }

        public object GetVertexAugmentation(ConnVertex vertex)
        {
            var info = EnsureInfo(vertex);
            return info.aug;
        }

        public ComponentInfo GetComponentInfo(ConnVertex vertex)
        {
            long iteration = ++_iteration;

            var info = EnsureInfo(vertex);
            info.iteration = iteration;

            int size = 1;
            var queue = new Queue<VertexInfo>(new[] {info});
            object aug = info.aug;
            while (queue.Count > 0)
            {
                var vert = queue.Dequeue();
                foreach (var next in vert.edges)
                {
                    if (next.iteration == iteration)
                        continue;

                    size++;
                    aug = _augmentation.Combine(aug, info.aug);
                    next.iteration = iteration;
                    queue.Enqueue(next);
                }
            }
            return new ComponentInfo(vertex, aug, size);
        }

        public int GetNumberOfComponents()
        {
            int numberOfComponents = 0;
            long iteration = ++_iteration;

            var queue = new Queue<VertexInfo>();
            foreach (var pair in _info)
            {
                var info = pair.Value;
                if (info.iteration == iteration)
                    continue;

                numberOfComponents++;
                queue.Enqueue(info);
                while (queue.Count > 0)
                {
                    var vert = queue.Dequeue();
                    foreach (var next in vert.edges)
                    {
                        if (next.iteration == iteration)
                            continue;

                        next.iteration = iteration;
                        queue.Enqueue(next);
                    }
                }
            }

            return numberOfComponents;
        }

        public ICollection<ComponentInfo> GetAllComponents()
        {
            var list = new List<ComponentInfo>();
            long iteration = ++_iteration;

            var queue = new Queue<VertexInfo>();
            foreach (var pair in _info)
            {
                var info = pair.Value;
                if (info.iteration == iteration)
                    continue;

                int size = 1;
                var aug = info.aug;
                info.iteration = iteration;
                queue.Enqueue(info);
                while (queue.Count > 0)
                {
                    var vert = queue.Dequeue();
                    foreach (var next in vert.edges)
                    {
                        if (next.iteration == iteration)
                            continue;

                        size++;
                        aug = _augmentation.Combine(aug, next.aug);
                        next.iteration = iteration;
                        queue.Enqueue(next);
                    }
                }
                list.Add(new ComponentInfo(pair.Key, aug, size));
            }

            return list;
        }

        public bool VertexHasAugmentation(ConnVertex vertex)
        {
            var info = EnsureInfo(vertex);
            return info.aug != null;
        }

        public bool ComponentHasAugmentation(ConnVertex vertex)
        {
            return this.GetComponentAugmentation(vertex) != null;
        }

        public void Clear()
        {
            foreach (var pair in _info)
            {
                pair.Value.edges.Clear();
                pair.Value.iteration = 0;
            }
            _info.Clear();
        }

        public void Optimize()
        {
        }
    }
}
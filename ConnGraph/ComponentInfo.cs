namespace Connectivity
{
    public readonly struct ComponentInfo
    {
        /// <summary>
        /// An arbitrary vertex in the component
        /// </summary>
        public readonly ConnVertex vertex;

        /// <summary>
        /// The result of combining the augmentations associated with all of the vertices in the connected component.
        /// </summary>
        public readonly object augmentation;

        /// <summary>
        /// The number of vertices in the component.
        /// </summary>
        public readonly int size;

        /// <summary>
        /// Whether the component contains any vertex
        /// </summary>
        public bool Exists => size > 0;

        public ComponentInfo(ConnVertex vertex, object augmentation, int size)
        {
            this.vertex = vertex;
            this.augmentation = augmentation;
            this.size = size;
        }
    }
}
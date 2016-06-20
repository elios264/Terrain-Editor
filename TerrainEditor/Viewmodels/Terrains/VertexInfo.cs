using System.Windows;
using TerrainEditor.Core;

namespace TerrainEditor.Viewmodels.Terrains
{

    public class VertexInfo : PropertyChangeBase
    {
        private Vector m_position;
        private VertexDirection m_direction = VertexDirection.Auto;
        private SplitMode m_split;

        public Vector Position
        {
            get { return m_position; }
            set
            {
                if (value.Equals(m_position)) return;
                m_position = value;
                OnPropertyChanged();
            }
        }
        public VertexDirection Direction
        {
            get { return m_direction; }
            set
            {
                if (value == m_direction) return;
                m_direction = value;
                OnPropertyChanged();
            }
        }
        public SplitMode Split
        {
            get { return m_split; }
            set
            {
                if (value == m_split)
                    return;
                m_split = value;
                OnPropertyChanged();
            }
        }

        public VertexInfo(double x = 0, double y = 0)
        {
            Position = new Vector(x, y);
        }

        public VertexInfo(Vector pos, VertexDirection dir, SplitMode split)
        {
            Position = pos;
            Direction = dir;
            Split = split;
        }
    }

}
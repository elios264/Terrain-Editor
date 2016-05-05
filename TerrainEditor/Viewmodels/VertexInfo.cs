using System.Windows;

namespace TerrainEditor.ViewModels
{
    public class VertexInfo : ViewModelBase
    {
        private Vector m_position;
        private VertexDirection m_direction;

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

        public VertexInfo(double x = 0, double y = 0, VertexDirection dir = VertexDirection.Auto)
        {
            Position = new Vector(x, y);
            Direction = dir;
        }
    }
}
namespace EternalSleep.UI
{
    /// <summary>Una página extra de la pausa (diario, estadísticas, logros) que se abre desde el menú principal.</summary>
    public interface IPausePage
    {
        /// <summary>Título grande de la pantalla.</summary>
        string Heading { get; }
        /// <summary>Texto de ayuda de abajo.</summary>
        string Help { get; }
        void Show();
        void Hide();
        /// <summary>W/S: baja (+1) o sube (-1). Las páginas que caben enteras no hacen nada.</summary>
        void Scroll(int direction);
    }
}

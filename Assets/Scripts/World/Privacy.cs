using System;

namespace ErvasDoBrasil
{
    /// <summary>
    /// Caminhos mostrados na tela ou gravados no log trocam a pasta pessoal por "~": uma captura de tela
    /// ou um log enviado num relato de bug não revela o nome da conta no computador. Ver docs/15-seguranca.md.
    /// </summary>
    public static class Privacy
    {
        static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static string Redact(string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(Home) || Home.Length < 2) return text;
            return text.Replace(Home, "~");
        }
    }
}

#if UNITY_EDITOR
namespace RealisticClassroom.Publishing
{
    public static class PublishingPipeline
    {
        /// <summary>Re-creates every converted asset from the source downloads (models, characters, architecture, animations).</summary>
        public static string ConvertAll()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(ModelConverter.ConvertAllStatics());
            sb.AppendLine(ModelConverter.ConvertAllCharacters());
            sb.AppendLine(ArchitectureConverter.ConvertAll());
            sb.AppendLine(AnimationSetup.Run());
            return sb.ToString();
        }
    }
}
#endif

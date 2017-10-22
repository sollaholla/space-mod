namespace BaseBuilding.Serialization
{
    public class ResourceDefinition
    {
        public ResourceDefinition()
        {
            RockInfo = new RockInfo();
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public string ResourceColor { get; set; }

        public RockInfo RockInfo { get; set; }
    }
}
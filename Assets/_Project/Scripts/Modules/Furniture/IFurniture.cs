#nullable enable

namespace GeminiLab.Modules.Furniture
{
    public interface IFurniture
    {
        string InstanceId { get; }

        FurnitureDefinitionSO Definition { get; }

        InteractionAnchor Anchor { get; }
    }
}

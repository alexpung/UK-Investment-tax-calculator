namespace Model.Interfaces;

public interface IAssetDatedEvent
{
    string AssetName { get; }
    DateTime Date { get; }
}
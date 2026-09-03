using DeepCore.Game3D.Slave.Runtime;

namespace Code.Battle;

public interface IBattleHUD
{
    public AbstractBattle Battle { get; }
    public void Init(AbstractBattle battle);
}
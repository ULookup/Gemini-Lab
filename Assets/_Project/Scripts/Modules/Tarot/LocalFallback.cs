#nullable enable
using System.Text;
using GeminiLab.Modules.Pet;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// Gateway 未就绪或失败时的本地占位解读：用牌面关键词 + 宠物人格模板拼接一段文本。
    /// 真实玩法阶段 Gateway 上线后这条路径会很少触发；作为兜底保证 UI 流程不中断。
    /// </summary>
    public static class LocalFallback
    {
        public static TarotReading Build(TarotDrawResult draw, PetId petId, TarotOrientation orientation)
        {
            var card = draw.Card;
            var keywords = card.GetKeywords(orientation);
            string kwJoined = keywords.Count > 0 ? string.Join(" · ", keywords) : "";

            StringBuilder sb = new();
            if (petId == PetId.Angel)
            {
                sb.Append("（天使视角）").Append(card.DisplayNameZh).Append(orientation == TarotOrientation.Upright ? "·正位：" : "·逆位：");
                if (!string.IsNullOrEmpty(kwJoined))
                {
                    sb.Append(kwJoined).Append('。');
                }
                sb.Append(orientation == TarotOrientation.Upright
                    ? "愿光照亮你今日的路。"
                    : "阴影里也藏着值得你注意的提醒。");
            }
            else
            {
                sb.Append("（恶魔视角）").Append(card.DisplayNameZh).Append(orientation == TarotOrientation.Upright ? "·正位：" : "·逆位：");
                if (!string.IsNullOrEmpty(kwJoined))
                {
                    sb.Append(kwJoined).Append('。');
                }
                sb.Append(orientation == TarotOrientation.Upright
                    ? "别被光晃得太舒服，今天还有许多欲望值得你承认。"
                    : "没错，就是你以为的那样——但你敢承认吗？");
            }

            return new TarotReading(petId, orientation, sb.ToString(), isFromGateway: false);
        }
    }
}

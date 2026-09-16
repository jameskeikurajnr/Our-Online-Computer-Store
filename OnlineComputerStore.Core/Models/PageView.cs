using System;

namespace OnlineComputerStore.Core.Models
{
    // One row per real page request, recorded by PageViewMiddleware. This is the
    // store's own minimal traffic log — it exists only to feed the "Traffic"
    // line on the admin dashboard (previously hardcoded sample data), so it
    // deliberately stores nothing that identifies who made the request: no IP
    // address, no cookie/session id, no user agent. Just what was viewed and
    // when, which is enough to count visits per day.
    public class PageView
    {
        public int Id { get; set; }
        public string Path { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

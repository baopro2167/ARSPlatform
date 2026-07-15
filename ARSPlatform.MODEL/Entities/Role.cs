using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}

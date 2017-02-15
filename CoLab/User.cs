using System;
using System.Collections.Generic;
using LiteDB;

namespace CoLab
{
    public class User
    {
        public User()
        {
            
        }
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string PassHash { get; set; }

        public List<ProjInf> Projects { get; set; } = new List<ProjInf>();
        public List<ProjInf> CollaboratorOn { get; set; } = new List<ProjInf>();
        public List<ProjectInvite> PendingInvites { get; set; } = new List<ProjectInvite>();
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DeveloperType { get; set; }
        public string CountryCode { get; set; }
        public DateTime JoinedUtc { get; set; } = DateTime.UtcNow;

        public void AcceptInvite(ProjectInvite pinv)
        {
            CollaboratorOn.Add(new ProjInf(pinv.Id, pinv.Name));
        }
    }

    public class ProjectInvite
    {
        public ProjectInvite()
        {
            
        }
        public string Id { get; set; }
        public string Name { get; set; }
        public string InviterId { get; set; }
        public string InviterName { get; set; }
    }
}
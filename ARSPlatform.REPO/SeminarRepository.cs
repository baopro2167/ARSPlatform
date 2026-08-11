using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPOSITORIES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARSPlatform.REPO
{
    public class SeminarRepository : GenericRepository<Seminar>, ISeminarRepository
    {
        public SeminarRepository(AppDbContext context) : base(context)
        {
        }
    }
}
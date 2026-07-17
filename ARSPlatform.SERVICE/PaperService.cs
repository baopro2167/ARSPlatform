using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.ExternalServices;
using ARSPlatform.SERVICE.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICES
{
    public class PaperService : IPaperService
    {
        private readonly IPaperRepository _paperRepository;
        private readonly IExternalApiService _externalApiService;
        private readonly IMapper _mapper;

        public PaperService(IPaperRepository paperRepository, IExternalApiService externalApiService, IMapper mapper)
        {
            _paperRepository = paperRepository;
            _externalApiService = externalApiService;
            _mapper = mapper;
        }

        public async Task<PagedResult<PaperResponse>> GetPapersAsync(PaginationParams paginationParams)
        {
            var query = _paperRepository.GetQueryable()
                .Include(p => p.Creator)
                .AsNoTracking();

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<PaperResponse>>(items);

            return new PagedResult<PaperResponse>(dtos, totalCount, paginationParams.PageNumber, paginationParams.PageSize);
        }

        public async Task<PaperResponse?> GetPaperByIdAsync(int id)
        {
            var paper = await _paperRepository.GetWithAuthorByIdAsync(id);
            return paper != null ? _mapper.Map<PaperResponse>(paper) : null;
        }

        public async Task<PaperResponse> CreatePaperAsync(PaperCreateRequest request, int authorId)
        {
            var paper = _mapper.Map<Paper>(request);
            paper.CreatorId = authorId;
            paper.Status = "Submitted";
            paper.CreatedAt = DateTime.UtcNow;

            await _paperRepository.AddAsync(paper);
            await _paperRepository.SaveChangesAsync();

            var createdPaper = await _paperRepository.GetWithAuthorByIdAsync(paper.PaperId);
            return _mapper.Map<PaperResponse>(createdPaper);
        }

        public async Task<PaperResponse?> UpdatePaperAsync(int id, PaperUpdateRequest request)
        {
            var paper = await _paperRepository.GetWithAuthorByIdAsync(id);
            if (paper == null)
                return null;

            paper.Title = request.Title;
            paper.Abstract = request.Abstract;
            paper.FileUrl = request.FileUrl;
            paper.Issn = request.Issn;
            paper.IsOpenAccess = request.IsOpenAccess;
            paper.Quartile = request.Quartile;
            paper.SubFieldId = request.SubFieldId;
            
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                paper.Status = request.Status;
            }

            paper.UpdatedAt = DateTime.UtcNow;

            _paperRepository.Update(paper);
            await _paperRepository.SaveChangesAsync();

            return _mapper.Map<PaperResponse>(paper);
        }

        public async Task<bool> DeletePaperAsync(int id)
        {
            var paper = await _paperRepository.GetByIdAsync(id);
            if (paper == null)
                return false;

            _paperRepository.Delete(paper);
            await _paperRepository.SaveChangesAsync();
            return true;
        }
    }
}

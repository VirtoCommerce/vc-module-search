using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Web.Controllers.Api;

[Route("api/search/index-field-settings")]
[Authorize(ModuleConstants.Security.Permissions.IndexManage)]
public class IndexFieldSettingController(
    IIndexFieldSettingService crudService,
    IIndexFieldSettingSearchService searchService,
    ISearchProvider searchProvider)
    : Controller
{
    [HttpPost("search")]
    public async Task<ActionResult<IndexFieldSettingSearchResult>> Search([FromBody] IndexFieldSettingSearchCriteria criteria)
    {
        var result = await searchService.SearchNoCloneAsync(criteria);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<IndexFieldSetting>> Save([FromBody] IndexFieldSetting model)
    {
        await crudService.SaveChangesAsync([model]);
        return Ok(model);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IndexFieldSetting>> Get([FromRoute] string id, [FromQuery] string responseGroup = null)
    {
        var model = await crudService.GetNoCloneAsync(id, responseGroup);
        return Ok(model);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Delete([FromQuery] string[] ids)
    {
        await crudService.DeleteAsync(ids);
        return NoContent();
    }

    [HttpGet("~/api/search/field-values")]
    public async Task<ActionResult<string[]>> GetFacetValues([FromQuery] string documentType, [FromQuery] string fieldName)
    {
        if (string.IsNullOrEmpty(documentType) || string.IsNullOrEmpty(fieldName))
        {
            return Ok(Array.Empty<string>());
        }

        var searchRequest = new SearchRequest
        {
            Aggregations = [new TermAggregationRequest { FieldName = fieldName, Size = 1000 }],
            Take = 0,
        };

        var searchResponse = await searchProvider.SearchAsync(documentType, searchRequest);

        var values = searchResponse.Aggregations.FirstOrDefault()?.Values
            .Select(x => x.Id)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? [];

        return Ok(values);
    }
}

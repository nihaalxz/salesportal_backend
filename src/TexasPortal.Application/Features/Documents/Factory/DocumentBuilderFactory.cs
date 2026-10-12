using System;
using System.Collections.Generic;
using System.Linq;
using TexasPortal.Application.Features.Documents.Builders;

namespace TexasPortal.Application.Features.Documents.Factory;

public interface IDocumentBuilderFactory
{
    IDocumentBuilder? GetBuilder(string category, string type);
}

public class DocumentBuilderFactory : IDocumentBuilderFactory
{
    private readonly IEnumerable<IDocumentBuilder> _builders;

    public DocumentBuilderFactory(IEnumerable<IDocumentBuilder> builders)
    {
        _builders = builders;
    }

    public IDocumentBuilder? GetBuilder(string category, string type)
    {
        return _builders.FirstOrDefault(b => b.CanBuild(category, type));
    }
}

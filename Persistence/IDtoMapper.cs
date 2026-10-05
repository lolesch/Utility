namespace Submodules.Utility.Persistence
{
    /// <summary>
    /// The contract between a domain object and the plain Dto that saves. A separate object, not a
    /// base class: it can take a catalog to resolve ids, and the domain type stays immutable because
    /// <see cref="ToDomain"/> builds a new instance instead of filling one in place. The mapper
    /// holds no state of its own beyond what it was built with.
    /// </summary>
    public interface IDtoMapper<TDomain, TDto>
    {
        TDto ToDto(TDomain domain);

        TDomain ToDomain(TDto dto);
    }
}

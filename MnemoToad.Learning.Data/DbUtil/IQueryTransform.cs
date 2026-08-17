namespace MnemoToad.Learning.Data.DbUtil;

public interface IQueryTransform<TSource, TDestination>
{
    IQueryable<TDestination> Transform(IQueryable<TSource> source, string name);
}

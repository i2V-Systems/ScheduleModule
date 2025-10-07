

using System.Linq.Expressions;

namespace Application.Schedule;

public interface IScheduleRepository <T>
    where T : class, new()
{
    void Add(T entity, Guid userId);
        void DetachEntity(T entity);
        Task AddAsync(T entity, Guid userId);
        Task AddRange(List<T> entities, Guid userId);
        int Count();
        Task<int> CountAsync();

        int CountWhere(Expression<Func<T, bool>> predicate);
        void Delete(T entity, Guid userId);
        void DeleteRange(List<T> entities, Guid userId);
        List<T> DeleteWhere(Expression<Func<T, bool>> predicate, Guid userId);

        T Get(Guid id);
        Task<T> GetAsync(Guid id);
        List<T> GetAll();
        Task<List<T>> GetAllAsync();
        Task<List<T>> GetAllIncludingAsync(params Expression<Func<T, object>>[] includeProperties);

        T Find(Expression<Func<T, bool>> match);
        Task<T> FindAsync(Expression<Func<T, bool>> match);
        Task<T> FindAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includeProperties
        );

        IEnumerable<T> GetAllWithThenInclude(
            Expression<Func<T, bool>> match,
            Func<IQueryable<T>, IQueryable<T>> func
        );
        IEnumerable<T> GetAllWithThenIncludeNoCondition(Func<IQueryable<T>, IQueryable<T>> func);

        T GetWithThenInclude(
            Expression<Func<T, bool>> match,
            Func<IQueryable<T>, IQueryable<T>> func
        );

        List<T> FindAll(Expression<Func<T, bool>> match);
        List<T> FindAll(
            Expression<Func<T, bool>> match,
            params Expression<Func<T, object>>[] includeProperties
        );
        Task<List<T>> FindAllAsync(Expression<Func<T, bool>> match);
        Task<List<T>> FindAllAsync(
            Expression<Func<T, bool>> match,
            params Expression<Func<T, object>>[] includeProperties
        );
        Task<List<T>> FindAllAsync(
            Expression<Func<T, bool>> match,
            List<Expression<Func<T, object>>> includeProperties
        );
        void Update(T entity, Guid userId);
        void UpdateRange(List<T> entities, Guid userId);

        void Dispose();

        List<T> GetAllWithSkipTakeThenInclude(
            int skip,
            int pageLimit,
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includeProperties
        );
    
}
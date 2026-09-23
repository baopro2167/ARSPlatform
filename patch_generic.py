import re

interface_path = 'd:/CapstoneProject2026/ARSPlatform/ARSPlatform.REPO/Interfaces/IGenericRepository.cs'
with open(interface_path, 'r', encoding='utf-8') as f:
    text = f.read()
if 'CountAsync' not in text:
    text = text.replace('Task SaveChangesAsync();', 'Task SaveChangesAsync();\n        Task<int> CountAsync(Expression<Func<T, bool>> predicate = null);\n        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate = null);\n        Task<T?> FindAsync(Expression<Func<T, bool>> predicate = null);')
    with open(interface_path, 'w', encoding='utf-8') as f:
        f.write(text)

class_path = 'd:/CapstoneProject2026/ARSPlatform/ARSPlatform.REPO/GenericRepository.cs'
with open(class_path, 'r', encoding='utf-8') as f:
    text = f.read()
if 'CountAsync(Expression' not in text:
    methods = """
        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null)
        {
            return predicate == null ? await _dbSet.CountAsync() : await _dbSet.CountAsync(predicate);
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate = null)
        {
            return predicate == null ? await _dbSet.AnyAsync() : await _dbSet.AnyAsync(predicate);
        }

        public virtual async Task<T?> FindAsync(Expression<Func<T, bool>> predicate = null)
        {
            return predicate == null ? await _dbSet.FirstOrDefaultAsync() : await _dbSet.FirstOrDefaultAsync(predicate);
        }
"""
    text = text.replace('        public virtual async Task SaveChangesAsync()', methods + '        public virtual async Task SaveChangesAsync()')
    with open(class_path, 'w', encoding='utf-8') as f:
        f.write(text)

print('Patched Generic')

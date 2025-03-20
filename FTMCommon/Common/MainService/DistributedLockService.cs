using FTM.Common.Entities;
using FTMPlus.Common.Db;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using static Amazon.S3.Util.S3EventNotification;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace FTMPlus.Common.MainService
{

    public enum LockValue
    {
        None = -1,
        Start = 0,
        Processing = 1,
        Processed = 2,
        Error = 3
    }
    public class DistributedLockService
    {
        private readonly IDatabase _redis;
        private readonly ListenerContext _dbContext;
        private string _prefix = "LOCK:";

        public DistributedLockService(ConnectionMultiplexer redis, ListenerContext dbContext)
        {
            _redis = redis.GetDatabase();
            _dbContext = dbContext;
        }

        public string getLockKey(string key)
        {
            return (_prefix + key).ToUpper();
        }

        private async Task<LockValue> GetExistingLockValueDbAsync(string key)
        {
            var lockValue = (await _dbContext.FileProcessingLocks.FirstOrDefaultAsync(x => x.LockKey == key))?.LockValue;
            if (lockValue == null)
                return LockValue.None;
            Enum.TryParse<LockValue>(lockValue, out var convert); 
            return convert;

        }
        /// <summary>
        /// Belirtilen lock var mı kontrol eder. Varsa değerini döner, yoksa null döner.
        /// </summary>
        public async Task<LockValue> GetExistingLockValueAsync(string key,bool isOnlyRedis=false)
        {
            try
            {
                key = getLockKey(key);
                var existingValue = await _redis.StringGetAsync(key);
                if(existingValue.IsNull && isOnlyRedis)
                {
                    return LockValue.None;
                }
                if (existingValue.IsNull)
                    return await GetExistingLockValueDbAsync(key);
                Enum.TryParse<LockValue>(existingValue, out var convert);
                return convert;
            }
            catch (Exception ex)
            {
                return await GetExistingLockValueDbAsync(key);
            }


            //return existingValue.HasValue ? existingValue.ToString() : -1;
        }

        /// <summary>
        /// Lock'u kaldırır (Redis + MSSQL).
        /// </summary>
        public async Task ReleaseLockAsync(string key,bool isOnlyRedis=false)
        {
            key = getLockKey(key);
            await _redis.KeyDeleteAsync(key);
            if (!isOnlyRedis)
            {
                await _dbContext.FileProcessingLocks.Where(x => x.LockKey == key).Take(1).ExecuteDeleteAsync();
                await _dbContext.SaveChangesAsync();
            }
            Console.WriteLine($"Lock kaldırıldı: {key}");
        }

        /// <summary>
        /// Lock mekanizmasını çalıştırır. Önce mevcut lock olup olmadığını kontrol eder.
        /// Eğer lock varsa, mevcut lock değerini döndürür. Yoksa yeni bir lock alır.
        /// </summary>
        public async Task<LockValue?> AcquireLockAsync(string key, int lockTimeoutSeconds = 30, LockValue value = LockValue.Start,bool isOnlyRedis=false, bool setFoceValue = false)
        {
            // 1. Var olan lock kontrolü
            var existingLock = await GetExistingLockValueAsync(key,isOnlyRedis);
            if (existingLock != LockValue.None && !setFoceValue)
            {
                Console.WriteLine($"Mevcut lock bulundu: {key} -> {existingLock}");
                return existingLock; // Eğer lock varsa, mevcut lock'un değerini döndür.
            }
            key = getLockKey(key);
            // 2. Redis Lock - SETNX ile hızlı lock kontrolü
            bool redisLock = await _redis.StringSetAsync(key, value.ToString(), TimeSpan.FromSeconds(lockTimeoutSeconds), setFoceValue?When.Always: When.NotExists);
            if (!redisLock && !setFoceValue)
            {
                Console.WriteLine($"Redis lock alınamadı: {key}");
                return await GetExistingLockValueAsync(key,isOnlyRedis); // Lock başarısız olursa tekrar kontrol et.
            }
            if(isOnlyRedis)
            {
                return value;
            }
            // 3. MSSQL Lock (EF Core ile)
            try
            {
                var up = await _dbContext.FileProcessingLocks.FirstOrDefaultAsync(t => t.LockKey == key);
                if (up != null)
                {
                    up.LockValue = value.ToString();
                    up.IsActive = true;

                    _dbContext.FileProcessingLocks.Attach(up);
                    _dbContext.Entry(up).State = EntityState.Deleted;
                    await _dbContext.SaveChangesAsync();

                }
                else
                {
                    var lockEntry = new FileProcessingLock { LockKey = key, LockValue = value.ToString(), IsActive = true };
                    _dbContext.FileProcessingLocks.Attach(lockEntry);
                    _dbContext.Entry(lockEntry).State = EntityState.Added;
                    await _dbContext.SaveChangesAsync();
                }
                Console.WriteLine($"Lock başarıyla alındı: {key}");
                return value;
            }
            catch (DbUpdateException ex)
            {
                // Eğer SQL tarafında UNIQUE constraint hatası alırsak (lock zaten alınmış), Redis lock'u kaldır
                await _redis.KeyDeleteAsync(key);
                Console.WriteLine($"Veritabanı lock alınamadı: {key}");
                return await GetExistingLockValueAsync(key,isOnlyRedis);
            }
        }
         
        public string getLockHash(object item)
        {
            string json = JsonSerializer.Serialize(item);
            using (SHA256 sha1 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                byte[] hashBytes = sha1.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        public string getLockBase(string hash)
        {
            if (hash.StartsWith(_prefix))
            {
                return hash.Substring(_prefix.Length);
            }
            return hash;
        }
    } 
    public class DistributedLockMasterService
    {
        private readonly IDatabase _redis;
        private string _prefix = "lockFiles:";
        public DistributedLockMasterService(ConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }

        public string getLockKey(string key)
        {
            return _prefix + key;
        }
        /// <summary>
        /// Belirtilen lock var mı kontrol eder. Varsa değerini döner, yoksa null döner.
        /// </summary>
        public async Task<string?> GetExistingLockValueAsync(string key, int lockTimeoutSeconds = 30)
        {
            key = getLockKey(key);
            var existingValue = await _redis.StringGetAsync(key);
            return existingValue.HasValue ? existingValue.ToString() : null;
        }

        /// <summary>
        /// Lock mekanizmasını çalıştırır. Önce mevcut lock olup olmadığını kontrol eder.
        /// Eğer lock varsa, mevcut lock değerini döndürür. Yoksa yeni bir lock alır.
        /// </summary>
        public async Task<bool> AcquireLockAsync(string key, int lockTimeoutSeconds = 30, string value = "1")
        {
            key = getLockKey(key);
            bool redisLock = await _redis.StringSetAsync(key, value, TimeSpan.FromSeconds(lockTimeoutSeconds));
            return redisLock;
        }

        /// <summary>
        /// Lock'u kaldırır (Redis).
        /// </summary>
        public async Task ReleaseLockAsync(string key)
        {
            key = getLockKey(key);
            await _redis.KeyDeleteAsync(key);
        }


    }

}
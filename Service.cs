using Microsoft.EntityFrameworkCore;
using TodoApi;

public class Service
{
    private readonly ToDoDbContext _db;

    // הזרקת מסד הנתונים ישירות לתוך השירות
    public Service(ToDoDbContext db)
    {
        _db = db;
    }

    // שליפת כל הפריטים
    public async Task<List<Item>> GetAllItemsAsync()
    {
        return await _db.Items.ToListAsync();
    }

    // שליפת פריט בודד לפי מזהה
    public async Task<Item?> GetItemByIdAsync(int id)
    {
        return await _db.Items.FindAsync(id);
    }

    // יצירת פריט חדש
    public async Task<Item> CreateItemAsync(Item newItem)
    {
        _db.Items.Add(newItem);
        await _db.SaveChangesAsync();
        
        return newItem; 
    }

    // עדכון פריט קיים
public async Task<bool> UpdateItemAsync(int id, Item updatedItem)
{
    var item = await _db.Items.FindAsync(id);
    if (item is null) return false; 

    // הוספנו את השורה הזו כדי שגם הסטטוס יתעדכן במסד הנתונים!
    item.IsComplete = updatedItem.IsComplete; 
    
    // אם רוצים שיהיה אפשר לעדכן גם את השם מאותה פונקציה:
    if (!string.IsNullOrEmpty(updatedItem.Name))
    {
        item.Name = updatedItem.Name;
    }

    await _db.SaveChangesAsync();
    
    return true; 
}

    // מחיקת פריט
    public async Task<bool> DeleteItemAsync(int id)
    {
        var item = await _db.Items.FindAsync(id);
        if (item is null) return false;

        _db.Items.Remove(item);
        await _db.SaveChangesAsync();
        
        return true;
    }

    public async Task<User?> AuthenticateUser(string? name, string? password)
{
    return await _db.Users
        .FirstOrDefaultAsync(u => u.Name == name && u.Password == password);
}
}
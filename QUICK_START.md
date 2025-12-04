# Quick Start: Connecting Hotel Web & HotelManagement

## ✅ What's Been Done

1. **CORS Configuration Added** - `HotelManagement/Program.cs` now allows API calls from React app
2. **Connection Guide Created** - See `CONNECTION_GUIDE.md` for detailed instructions
3. **Example API File Created** - `Hotel Web/src/utils/api-backend.ts.example` shows how to connect

## 🚀 Quick Start (3 Steps)

### Step 1: Start Backend
```bash
cd E:\Work\Adapcore\HotelManagement
dotnet run
```
Backend will run on: `http://localhost:60713`

### Step 2: Test API Endpoint
Open browser and visit:
```
http://localhost:60713/api/Menu/getItems
```
You should see JSON data (or empty array if no data).

### Step 3: Connect React App

**Option A: Use Example File**
1. Copy `Hotel Web/src/utils/api-backend.ts.example` to `Hotel Web/src/utils/api-backend.ts`
2. Update `api.ts` to import from `api-backend.ts` instead of using static JSON

**Option B: Update Existing api.ts**
1. Add API base URL:
   ```typescript
   const API_BASE_URL = 'http://localhost:60713';
   ```
2. Replace static JSON calls with fetch calls:
   ```typescript
   export async function fetchRooms() {
     const response = await fetch(`${API_BASE_URL}/api/Room/GetRoomCategories`);
     return await response.json();
   }
   ```

## 📋 Available API Endpoints

- `GET /api/Menu/getItems` - Get menu items
- `GET /api/Room/GetRoomCategories` - Get room categories
- `GET /api/TourType/getItems` - Get tour packages
- `GET /api/Laundry/getItems` - Get laundry services
- `GET /api/OtherType/getItems` - Get other services

## 🔧 Configuration

### Environment Variables (Optional)
Create `Hotel Web/.env`:
```
VITE_API_URL=http://localhost:60713
```

### CORS Origins
Currently configured for:
- `http://localhost:3000` (Vite)
- `http://localhost:5173` (Vite alt)
- `http://localhost:60713` (Backend HTTP)
- `https://localhost:44343` (Backend HTTPS)

To add production URLs, update `Program.cs` CORS configuration.

## ⚠️ Important Notes

1. **Data Structure Mismatch**: The backend APIs return simple `{Id, Name}` objects, but your React app expects richer data (price, images, etc.). You'll need to:
   - Extend backend models to include more fields, OR
   - Transform data in the React app, OR
   - Create new API endpoints that return the full data structure

2. **Start Backend First**: Always start the backend before the frontend to avoid connection errors.

3. **CORS Errors**: If you see CORS errors, verify:
   - Backend is running
   - Frontend URL matches one of the allowed origins
   - CORS middleware is enabled (it is, in `Program.cs`)

## 📚 Next Steps

1. Review `CONNECTION_GUIDE.md` for detailed options
2. Check `api-backend.ts.example` for implementation examples
3. Extend backend APIs to return full data structures needed by React app
4. Add error handling and loading states in React app
5. Configure production URLs when ready to deploy

## 🆘 Troubleshooting

**CORS Error?**
- Check backend is running
- Verify frontend URL is in allowed origins
- Check browser console for specific error

**API Returns 404?**
- Verify endpoint URL is correct
- Check backend is running
- Verify route is registered

**No Data Returned?**
- Check database has data
- Verify Umbraco content exists (for Umbraco-based endpoints)
- Check API response in browser Network tab


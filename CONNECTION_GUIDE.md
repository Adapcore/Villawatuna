# Hotel Web & HotelManagement Connection Guide

This guide explains how to connect the **Hotel Web** (React frontend) with **HotelManagement** (ASP.NET Core backend).

## Project Structure

- **HotelManagement**: ASP.NET Core MVC + Umbraco CMS (Backend/Admin)
  - Location: `E:\Work\Adapcore\HotelManagement`
  - Port: HTTP: `60713`, HTTPS: `44343` (from launchSettings.json)
  - APIs: `/api/*` endpoints
  - Example: `http://localhost:60713/api/Menu/getItems`

- **Hotel Web**: React + TypeScript + Vite (Frontend Website)
  - Location: `E:\Work\Adapcore\Hotel Web`
  - Port: 3000 (Vite dev server)
  - Currently uses static JSON data

## Connection Options

### Option 1: API-Based Connection (Recommended) ✅

**Keep projects separate, connect via APIs**

#### Setup Steps:

1. **Enable CORS in HotelManagement** (Already done in Program.cs)
   - CORS is configured to allow requests from `http://localhost:3000` and `http://localhost:5173`

2. **Update React App API Configuration**
   - Update `Hotel Web/src/utils/api.ts` to call HotelManagement APIs
   - Example API endpoints available:
     - `/api/Menu/getItems` - Get menu items
     - `/api/Room/GetRoomCategories` - Get room categories
     - `/api/TourType/getItems` - Get tour items
     - `/api/Laundry/getItems` - Get laundry items

3. **Update React App to Use APIs**
   ```typescript
   // In Hotel Web/src/utils/api.ts
   const API_BASE_URL = 'http://localhost:5000'; // Update with your backend URL
   
   export async function fetchRooms() {
     const response = await fetch(`${API_BASE_URL}/api/Room/GetRoomCategories`);
     return await response.json();
   }
   ```

#### Pros:
- ✅ Independent deployment
- ✅ Clear separation of concerns
- ✅ Can scale independently
- ✅ Easy to maintain

#### Cons:
- ⚠️ Need to configure CORS
- ⚠️ Separate hosting required

---

### Option 2: Integrated Deployment (Single Project)

**Build React app and serve from HotelManagement wwwroot**

#### Setup Steps:

1. **Build React App**
   ```bash
   cd "E:\Work\Adapcore\Hotel Web"
   npm run build
   ```

2. **Copy Build Output to HotelManagement**
   - Copy `Hotel Web/dist/*` to `HotelManagement/wwwroot/hotel-web/`

3. **Configure Static Files in Program.cs**
   ```csharp
   app.UseStaticFiles(); // Already configured
   ```

4. **Add Route to Serve React App**
   - Add a controller/view to serve the React app for public routes

#### Pros:
- ✅ Single deployment
- ✅ No CORS issues
- ✅ Shared domain

#### Cons:
- ⚠️ Tight coupling
- ⚠️ Need to rebuild React on changes
- ⚠️ Larger deployment size

---

### Option 3: Shared Solution (Development)

**Add both projects to one Visual Studio solution**

#### Setup Steps:

1. **Add React Project to Solution** (as folder reference)
   - Right-click solution → Add → Existing Project
   - Note: React projects can't be added directly, but you can reference the folder

2. **Share Common Code**
   - Create a shared library project for DTOs/models
   - Both projects reference the shared library

#### Pros:
- ✅ Shared code/models
- ✅ Easier development workflow
- ✅ Type safety across projects

#### Cons:
- ⚠️ Still separate deployments
- ⚠️ More complex solution structure

---

## Recommended Approach: Option 1 (API-Based)

### Step-by-Step Implementation:

#### 1. Configure Backend API Endpoints

The following APIs are already available:
- `GET /api/Menu/getItems` - Menu items
- `GET /api/Room/GetRoomCategories` - Room categories  
- `GET /api/TourType/getItems` - Tour packages
- `GET /api/Laundry/getItems` - Laundry services

#### 2. Update React App API Configuration

Create/update `Hotel Web/src/utils/api.ts`:

```typescript
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:60713';

export async function fetchRooms() {
  try {
    const response = await fetch(`${API_BASE_URL}/api/Room/GetRoomCategories`);
    if (!response.ok) throw new Error('Failed to fetch rooms');
    const data = await response.json();
    // Transform data to match your React component expectations
    return { rooms: data };
  } catch (error) {
    console.error('Error fetching rooms:', error);
    return { rooms: [] };
  }
}

export async function fetchMenu() {
  try {
    const response = await fetch(`${API_BASE_URL}/api/Menu/getItems`);
    if (!response.ok) throw new Error('Failed to fetch menu');
    const data = await response.json();
    // Transform data as needed
    return { categories: data };
  } catch (error) {
    console.error('Error fetching menu:', error);
    return { categories: [] };
  }
}
```

#### 3. Create Environment Variables

Create `Hotel Web/.env`:
```
VITE_API_URL=http://localhost:60713
```

Or for HTTPS:
```
VITE_API_URL=https://localhost:44343
```

For production, update to your production API URL.

#### 4. Update CORS for Production

In `HotelManagement/Program.cs`, update CORS origins:
```csharp
policy.WithOrigins(
    "http://localhost:3000",
    "http://localhost:5173",
    "https://your-production-domain.com" // Add production URL
)
```

---

## Testing the Connection

1. **Start Backend**: Run HotelManagement project 
   - HTTP: `http://localhost:60713`
   - HTTPS: `https://localhost:44343`
   - Test API: `http://localhost:60713/api/Menu/getItems`

2. **Start Frontend**: Run `npm run dev` in Hotel Web folder (port 3000)

3. **Test API Call**: 
   - Open browser console
   - Check Network tab for API calls
   - Verify responses are received

4. **Verify CORS**: Ensure no CORS errors in browser console

---

## Next Steps

1. ✅ CORS is configured in Program.cs
2. ⏭️ Update React app's `api.ts` to use real APIs
3. ⏭️ Transform API responses to match React component data structures
4. ⏭️ Add error handling and loading states
5. ⏭️ Configure production URLs

---

## Need Help?

- Check API endpoints: `http://localhost:60713/api/Menu/getItems`
- Check CORS configuration in `Program.cs`
- Verify React app is calling correct API URLs
- Check browser console for CORS errors
- Verify backend is running before starting frontend
- Check that both projects are on the same network/localhost


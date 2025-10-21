// ✅ CORRECTED - Test Image Loading from Backend API
// File: test-image-loading.js (FIXED VERSION)
// Location: Frontend directory

console.log('🧪 Testing Image Loading from Backend API\n');
console.log('=' .repeat(60));

const BASE_URL = 'http://localhost:5000';
const API_ENDPOINT = '/api/Auctions'; // ✅ CORRECT ENDPOINT
const WRONG_ENDPOINT = '/api/auction/all'; // ❌ WRONG (what you had before)

console.log(`\n📌 ENDPOINT CORRECTION:`);
console.log(`   ❌ Wrong: ${BASE_URL}${WRONG_ENDPOINT}`);
console.log(`   ✅ Correct: ${BASE_URL}${API_ENDPOINT}\n`);
console.log('=' .repeat(60));

// Test function
async function testImageLoading() {
    try {
        console.log('\n1️⃣ Testing Backend Health...');
        const healthResponse = await fetch(`${BASE_URL}/api/health`);
        if (healthResponse.ok) {
            const health = await healthResponse.json();
            console.log('   ✅ Backend is running');
            console.log(`   📍 Status: ${health.status}`);
            console.log(`   📍 Environment: ${health.environment}`);
        } else {
            console.log('   ❌ Backend health check failed');
            return;
        }

        console.log('\n2️⃣ Fetching Auctions from CORRECT Endpoint...');
        console.log(`   🌐 URL: ${BASE_URL}${API_ENDPOINT}`);
        
        const response = await fetch(`${BASE_URL}${API_ENDPOINT}`);
        
        if (!response.ok) {
            console.error(`   ❌ Request failed: ${response.status} ${response.statusText}`);
            return;
        }

        const data = await response.json();
        console.log(`   ✅ Response received successfully`);
        
        if (!data.success) {
            console.error('   ❌ API returned error:', data.message);
            return;
        }

        const auctions = data.auctions || [];
        console.log(`   📦 Found ${auctions.length} auctions\n`);
        console.log('=' .repeat(60));

        if (auctions.length === 0) {
            console.log('\n⚠️  No auctions found in database');
            console.log('   Run the seeder or create auctions first\n');
            return;
        }

        // Test image URLs
        console.log('\n3️⃣ Testing Image URLs...\n');
        
        let totalImages = 0;
        let validImages = 0;
        let invalidImages = 0;

        for (const auction of auctions) {
            console.log(`\n📦 Auction #${auction.auctionId}: ${auction.title}`);
            console.log('-'.repeat(60));
            
            // Check primary image
            if (auction.primaryImageUrl) {
                totalImages++;
                const imageUrl = `${BASE_URL}${auction.primaryImageUrl}`;
                console.log(`   🖼️  Primary: ${auction.primaryImageUrl}`);
                console.log(`      Full URL: ${imageUrl}`);
                
                try {
                    const imgResponse = await fetch(imageUrl, { method: 'HEAD' });
                    if (imgResponse.ok) {
                        console.log(`      ✅ Image accessible (${imgResponse.headers.get('content-type')})`);
                        validImages++;
                    } else {
                        console.log(`      ❌ Image not found (${imgResponse.status})`);
                        invalidImages++;
                    }
                } catch (error) {
                    console.log(`      ❌ Error accessing image: ${error.message}`);
                    invalidImages++;
                }
            } else {
                console.log('   ⚠️  No primary image');
            }
            
            // Check additional images
            if (auction.imageUrls && auction.imageUrls.length > 1) {
                console.log(`   📸 Additional images: ${auction.imageUrls.length - 1}`);
                for (let i = 1; i < Math.min(auction.imageUrls.length, 3); i++) {
                    const imageUrl = `${BASE_URL}${auction.imageUrls[i]}`;
                    console.log(`      ${i}. ${auction.imageUrls[i]}`);
                    totalImages++;
                    
                    try {
                        const imgResponse = await fetch(imageUrl, { method: 'HEAD' });
                        if (imgResponse.ok) {
                            console.log(`         ✅ Accessible`);
                            validImages++;
                        } else {
                            console.log(`         ❌ Not found`);
                            invalidImages++;
                        }
                    } catch (error) {
                        console.log(`         ❌ Error: ${error.message}`);
                        invalidImages++;
                    }
                }
                
                if (auction.imageUrls.length > 3) {
                    console.log(`      ... and ${auction.imageUrls.length - 3} more`);
                }
            }
        }

        // Summary
        console.log('\n' + '='.repeat(60));
        console.log('\n📊 TEST SUMMARY\n');
        console.log(`   Total Auctions: ${auctions.length}`);
        console.log(`   Total Images Tested: ${totalImages}`);
        console.log(`   ✅ Valid Images: ${validImages}`);
        console.log(`   ❌ Invalid Images: ${invalidImages}`);
        
        if (invalidImages === 0 && totalImages > 0) {
            console.log('\n   🎉 SUCCESS! All images are loading correctly!\n');
        } else if (totalImages === 0) {
            console.log('\n   ⚠️  WARNING: No images found to test\n');
        } else {
            console.log('\n   ⚠️  Some images failed to load. Check file paths.\n');
        }
        
        console.log('=' .repeat(60));

    } catch (error) {
        console.error('\n❌ ERROR:', error.message);
        console.error('\n🔍 Troubleshooting:');
        console.error('   1. Is backend running on http://localhost:5000?');
        console.error('   2. Is the correct endpoint /api/Auctions (capital A)?');
        console.error('   3. Are images in wwwroot/uploads folder?');
        console.error('   4. Is static file middleware configured?');
        console.error('\n   Run: dotnet run --project WebApplication3.csproj\n');
    }
}

// Quick endpoint comparison
function showEndpointComparison() {
    console.log('\n📚 ENDPOINT REFERENCE:\n');
    console.log('   ✅ CORRECT ENDPOINTS:');
    console.log('      • GET  /api/Auctions          - List all auctions');
    console.log('      • GET  /api/Auctions/{id}     - Get single auction');
    console.log('      • POST /api/Auctions          - Create auction');
    console.log('      • GET  /api/ImageManagement/stats - Image statistics');
    console.log('      • GET  /uploaded/{filename}   - Access static images');
    console.log('      • GET  /api/health            - Health check\n');
    
    console.log('   ❌ WRONG ENDPOINTS:');
    console.log('      • /api/auction/all            - Does NOT exist!');
    console.log('      • /api/auctions               - Wrong case!');
    console.log('      • /uploads/{filename}         - Wrong path!\n');
    console.log('=' .repeat(60));
}

// Run tests
showEndpointComparison();
testImageLoading();

extends GutTest
## Unit tests for HexMetrics


func test_outer_radius_is_one():
	assert_eq(HexMetrics.OUTER_RADIUS, 1.0, "Outer radius should be 1.0")


func test_inner_radius_calculation():
	# Inner radius = outer * sqrt(3)/2 ≈ 0.866
	assert_almost_eq(HexMetrics.INNER_RADIUS, 0.866025404, 0.0001,
		"Inner radius should be outer * sqrt(3)/2")


func test_elevation_constants():
	assert_eq(HexMetrics.MIN_ELEVATION, 0, "Min elevation should be 0")
	assert_eq(HexMetrics.SEA_LEVEL, 4, "Sea level should be 4")
	assert_eq(HexMetrics.LAND_MIN_ELEVATION, 5, "Land min elevation should be 5")
	assert_eq(HexMetrics.MAX_ELEVATION, 13, "Max elevation should be 13")


func test_land_is_one_above_sea_level():
	assert_eq(HexMetrics.LAND_MIN_ELEVATION - HexMetrics.SEA_LEVEL, 1,
		"Land should be exactly 1 level above sea level")


func test_get_corners_returns_six_corners():
	var corners = HexMetrics.get_corners()
	assert_eq(corners.size(), 6, "Should return 6 corners")


func test_corners_are_at_outer_radius():
	var corners = HexMetrics.get_corners()
	for i in range(6):
		var distance = Vector2(corners[i].x, corners[i].z).length()
		assert_almost_eq(distance, HexMetrics.OUTER_RADIUS, 0.0001,
			"Corner %d should be at outer radius" % i)


func test_get_terrace_steps():
	# With TERRACES_PER_SLOPE = 2, steps = 2 * 2 + 1 = 5
	assert_eq(HexMetrics.get_terrace_steps(), 5, "Should have 5 terrace steps")


func test_terrace_lerp_at_zero():
	var a = Vector3(0, 0, 0)
	var b = Vector3(10, 5, 10)
	var result = HexMetrics.terrace_lerp(a, b, 0)
	assert_eq(result, a, "Step 0 should return start position")


func test_terrace_lerp_at_max():
	var a = Vector3(0, 0, 0)
	var b = Vector3(10, 5, 10)
	var steps = HexMetrics.get_terrace_steps()
	var result = HexMetrics.terrace_lerp(a, b, steps)
	assert_almost_eq(result.x, b.x, 0.0001, "Final step X should reach end")
	assert_almost_eq(result.z, b.z, 0.0001, "Final step Z should reach end")


func test_solid_and_blend_factors_sum_to_one():
	assert_almost_eq(HexMetrics.SOLID_FACTOR + HexMetrics.BLEND_FACTOR, 1.0, 0.0001,
		"Solid and blend factors should sum to 1.0")

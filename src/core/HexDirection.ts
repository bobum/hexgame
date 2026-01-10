// Six directions for hex neighbors (flat-topped hexes)
export enum HexDirection {
  NE = 0,  // Northeast
  E = 1,   // East
  SE = 2,  // Southeast
  SW = 3,  // Southwest
  W = 4,   // West
  NW = 5,  // Northwest
}

// Direction offsets in cube coordinates (q, r, s)
export const DirectionOffsets: Record<HexDirection, [number, number, number]> = {
  [HexDirection.NE]: [1, 0, -1],
  [HexDirection.E]: [1, -1, 0],
  [HexDirection.SE]: [0, -1, 1],
  [HexDirection.SW]: [-1, 0, 1],
  [HexDirection.W]: [-1, 1, 0],
  [HexDirection.NW]: [0, 1, -1],
};

// Get the opposite direction
export function opposite(dir: HexDirection): HexDirection {
  return (dir + 3) % 6;
}

// Get the next direction (clockwise)
export function next(dir: HexDirection): HexDirection {
  return (dir + 1) % 6;
}

// Get the previous direction (counter-clockwise)
export function previous(dir: HexDirection): HexDirection {
  return (dir + 5) % 6;
}

// Get next direction, skipping one (two steps clockwise)
export function next2(dir: HexDirection): HexDirection {
  return (dir + 2) % 6;
}

// Get previous direction, skipping one (two steps counter-clockwise)
export function previous2(dir: HexDirection): HexDirection {
  return (dir + 4) % 6;
}

// All directions as an array for iteration
export const AllDirections: HexDirection[] = [
  HexDirection.NE,
  HexDirection.E,
  HexDirection.SE,
  HexDirection.SW,
  HexDirection.W,
  HexDirection.NW,
];

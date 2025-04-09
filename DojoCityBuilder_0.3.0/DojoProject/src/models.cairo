use starknet::ContractAddress;

#[derive(Drop, Copy, Serde)]
#[dojo::model]
pub struct Building {
    #[key]
    pub player: ContractAddress,
    #[key]
    pub x: u32,
    #[key]
    pub y: u32,
    pub building_type: u32,
    pub residents: u32,
    pub jobs: u32,
    pub shopping_space: u32,
    pub happy_residents: u32,
    pub rotation: u32,
}

#[derive(Drop, Copy, Serde)]
#[dojo::model]
pub struct Tile {
    #[key]
    pub player: ContractAddress,
    #[key]
    pub x: u32,
    #[key]
    pub y: u32,
    pub tile_id: u64, // Non-key field
}

#[derive(Copy, Drop, Serde, starknet::Store)]
#[dojo::model]
pub struct Player {
    #[key]
    pub player: ContractAddress,
    pub money: u128,
}
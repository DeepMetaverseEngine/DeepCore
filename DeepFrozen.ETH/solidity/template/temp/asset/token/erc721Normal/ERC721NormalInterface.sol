// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface ERC721NormalInterface {

    event OwnershipTransferred(address oldManager, address newManager);

    function mint(address account, uint256 tokenId) external;

    function burn(uint256 tokenId) external;

    function exist(uint256 tokenId) view external returns (bool);

    function manager() external view returns (address);

    function transferOwnership(address newOwner) external;
}

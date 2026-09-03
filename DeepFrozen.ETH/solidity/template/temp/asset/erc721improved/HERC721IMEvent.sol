// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface HERC721IMEvent {

    event SetAccessControl(address accessControl);

    event SetSupport(bool supportTransfer, bool supportMint, bool supportBurn);

    event Mint(address indexed to, uint256 indexed tokenId);

    event Burn(address indexed from, uint256 indexed tokenId);

    event Freeze(address indexed unlocker, uint256 indexed tokenId);

    event Thaw(address indexed unlocker, uint256 indexed tokenId);

    event uint256Attribute(bytes32 attributeName, uint256 tokenId, uint256 attributeValue);

    event bytes32Attribute(bytes32 attributeName, uint256 tokenId, bytes32 attributeValue);

    event addressAttribute(bytes32 attributeName, uint256 tokenId, address attributeValue);

    event bytesAttribute(bytes32 attributeName, uint256 tokenId, bytes attributeValue);
}

// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface AssetVaultEvent {

    event MintErc20(address indexed tokenAddress, address indexed owner, uint256 amount);
    event DepositErc20(address indexed tokenAddress, address from, address indexed owner, uint256 amount);
    event WithdrawErc20(address indexed tokenAddress, address owner, uint256 amount, uint256 traceId);

    event MintErc1155(address indexed tokenAddress, address indexed owner, uint256 indexed tokenId, uint256 amount);
    event DepositErc1155(address indexed tokenAddress, address from, address indexed owner, uint256 indexed tokenId, uint256 amount);
    event WithdrawErc1155(address indexed tokenAddress, address indexed owner, uint256 indexed tokenId, uint256 amount, uint256 traceId);

    event MintErc721(address indexed tokenAddress, address indexed owner, uint256 indexed tokenId, bytes32[] attributeNames, uint256[] uint256Data, bytes32[] bytes32Data, address[] addressData, bytes[] bytesData);
    event MapErc721(address indexed tokenAddress, uint256 indexed tokenId, bytes32[] attributeNames, uint256[] uint256Data, bytes32[] bytes32Data, address[] addressData, bytes[] bytesData);
    event BurnErc721(address indexed tokenAddress, uint256 indexed tokenId);
    event DepositErc721(address indexed tokenAddress, address from, address indexed owner, uint256 tokenId);
    event WithdrawErc721(address indexed tokenAddress, address indexed owner, uint256 indexed tokenId, bytes32[] attributeNames, uint256[] uint256Data, bytes32[] bytes32Data, address[] addressData, bytes[] bytesData);
    event MeltErc721(address indexed tokenAddress, uint256 indexed tokenId);
    event SyncErc721(address indexed tokenAddress, uint256 indexed tokenId, bytes32[] attributeNames, uint256[] uint256Data, bytes32[] bytes32Data, address[] addressData, bytes[] bytesData);
}

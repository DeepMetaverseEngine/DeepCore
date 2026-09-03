// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../../nameserver/preclude/Preclude.sol";

import "@openzeppelin/contracts/token/ERC721/extensions/ERC721Enumerable.sol";

import "./ERC721NormalInterface.sol";

contract ERC721Normal is ERC721Enumerable, ERC721NormalInterface {

    using SafeMath for uint256;
    using EnumerableSet for EnumerableSet.AddressSet;
    using Counters for Counters.Counter;

    address internal _owner;

    EnumerableSet.AddressSet internal _managers;

    Counters.Counter internal _tokenIdCounter;

    mapping(bytes32 => mapping(uint256 => bytes32)) internal _fixedAttribute;
    mapping(bytes32 => mapping(uint256 => bytes)) internal _dynamicAttribute;

    constructor(
        string memory name_,
        string memory symbol_
    )
    ERC721(name_, symbol_){
        _owner = msg.sender;
        _managers.add(msg.sender);
    }

    modifier onlyOwner() {
        require(_owner == msg.sender, "onlyOwner");
        _;
    }

    modifier onlyAuth() {
        require(
            _owner == msg.sender ||
            _managers.contains(msg.sender),
            "onlyAuth"
        );
        _;
    }

    function mint(address account, uint256 tokenId) override external onlyAuth returns (uint256){

        if (tokenId == 0) {
            //starts from 1
            _tokenIdCounter.increment();
            tokenId = _tokenIdCounter.current();
        }

        require(tokenId != 0);
        _mint(account, tokenId);
        return tokenId;
    }

    function burn(uint256 tokenId) override external onlyAuth {
        _burn(tokenId);
    }

    function exist(uint256 tokenId) override view external returns (bool){
        return _exists(tokenId);
    }

    function setUint256Attribute(bytes32 attributeName, uint256 tokenId, uint256 attributeValue) override external onlyAuth {
        _fixedAttribute[attributeName][tokenId] = bytes32(attributeValue);
    }

    function setBytes32Attribute(bytes32 attributeName, uint256 tokenId, bytes32 attributeValue) override external onlyAuth {
        _fixedAttribute[attributeName][tokenId] = attributeValue;
    }

    function setAddressAttribute(bytes32 attributeName, uint256 tokenId, address attributeValue) override external onlyAuth {
        _fixedAttribute[attributeName][tokenId] = bytes32(uint256(uint160(attributeValue)));
    }

    function setBytesAttribute(bytes32 attributeName, uint256 tokenId, bytes memory attributeValue) override external onlyAuth {
        _dynamicAttribute[attributeName][tokenId] = attributeValue;
    }

    function getUint256Attribute(bytes32 attributeName, uint256 tokenId) override view external returns (uint256) {
        return uint256(_fixedAttribute[attributeName][tokenId]);
    }

    function getBytes32Attribute(bytes32 attributeName, uint256 tokenId) override view external returns (bytes32) {
        return _fixedAttribute[attributeName][tokenId];
    }

    function getAddressAttribute(bytes32 attributeName, uint256 tokenId) override view external returns (address)  {
        return address(uint160(uint256(_fixedAttribute[attributeName][tokenId])));
    }

    function getBytesAttribute(bytes32 attributeName, uint256 tokenId) override view external returns (bytes memory)  {
        return _dynamicAttribute[attributeName][tokenId];
    }

    function managers() override view public returns (address[] memory) {
        address[] memory ret = new address[](_managers.length());
        for (uint256 i = 0; i < _managers.length(); i++) {
            ret[i] = _managers.at(i);
        }
        return ret;
    }

    function setManager(address[] memory who, bool[] memory add) override external onlyOwner {
        for (uint256 i = 0; i < who.length; i++) {
            if (add[i]) {
                _managers.add(who[i]);
            } else {
                _managers.remove(who[i]);
            }
        }
    }

    function setAutoCounterBegin(uint256 offset) override external onlyOwner {
        _tokenIdCounter._value = offset;
    }

    function transferOwnership(address newOwner) override public onlyAuth {
        require(newOwner != address(0), "new owner is the zero address");
        emit OwnershipTransferred(_owner, newOwner);
        _owner = newOwner;
    }

    function _beforeTokenTransfer(
        address from,
        address to,
        uint256 tokenId
    ) virtual override internal {
        require(tokenId != uint256(0), "tokenId can not be zero");

        super._beforeTokenTransfer(from, to, tokenId);
    }
}

// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "@openzeppelin/contracts/utils/math/SafeMath.sol";
import "@openzeppelin/contracts/token/ERC721/ERC721.sol";
import "@openzeppelin/contracts/token/ERC721/extensions/ERC721Enumerable.sol";

import "./ERC721NormalInterface.sol";

contract ERC721Normal is ERC721Enumerable, ERC721NormalInterface {

    using SafeMath for uint256;

    address internal _manager;

    constructor(
        string memory name_,
        string memory symbol_
    )
    ERC721(name_, symbol_){
        _manager = msg.sender;
    }

    modifier onlyManager() {
        require(_manager == msg.sender, "onlyManager");
        _;
    }

    function mint(address account, uint256 tokenId) override external onlyManager {
        _mint(account, tokenId);
    }

    function burn(uint256 tokenId) override external onlyManager {
        _burn(tokenId);
    }

    function exist(uint256 tokenId) override view external returns (bool){
        return _exists(tokenId);
    }

    function manager() override public view returns (address) {
        return _manager;
    }

    function transferOwnership(address newOwner) override public onlyManager {
        require(newOwner != address(0), "new owner is the zero address");
        emit OwnershipTransferred(_manager, newOwner);
        _manager = newOwner;
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
